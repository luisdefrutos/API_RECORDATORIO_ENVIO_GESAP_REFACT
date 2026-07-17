using RecordatorioEnvio.Infrastructure.Logging;
using RecordatorioEnvio.Infrastructure.Security;
using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace RecordatorioEnvio.API.Filters
{
    /// <summary>
    /// Limita las peticiones por IP (Ej: 10 peticiones por minuto).
    ///
    /// CÓMO FUNCIONA:
    ///   Se ejecuta automáticamente ANTES de cada acción del controlador donde esté decorado.
    ///   Mantiene en memoria un registro de timestamps por IP.
    ///   Si una IP supera el límite en la ventana de tiempo → devuelve 429 y corta el flujo.
    ///
    /// USO:
    ///   [RateLimit]                        → 10 req/60s (por defecto)
    ///   [RateLimit(limit: 5, windowSeconds: 30)] → 5 req/30s
    /// </summary>
    public class RateLimitAttribute : ActionFilterAttribute
    {
        // Memoria del servidor: IP → lista de timestamps de peticiones
        // (En producción con múltiples servidores, usar Redis en lugar de esto)
        private static readonly ConcurrentDictionary<string, ConcurrentQueue<DateTime>> _requestTracker =
            new ConcurrentDictionary<string, ConcurrentQueue<DateTime>>();

        // Evita loguear CADA petición bloqueada (si hay ataque, solo logueamos 1 vez por IP por ventana)
        private static readonly ConcurrentDictionary<string, DateTime> _lastLoggedBlock =
            new ConcurrentDictionary<string, DateTime>();

        // Control de limpieza periódica de memoria
        private static DateTime _lastCleanup = DateTime.UtcNow;
        private static readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);
        private static readonly object _cleanupLock = new object();

        private readonly int _limit;
        private readonly TimeSpan _window;

        public RateLimitAttribute(int limit = 10, int windowSeconds = 60)
        {
            _limit = limit;
            _window = TimeSpan.FromSeconds(windowSeconds);
        }

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            // 1. Limpieza periódica de IPs inactivas (cada 5 min) → evita memory leak
            LimpiarIPsInactivas();

            var ip = SecurityService.GetClientIp(actionContext.Request);
            var now = DateTime.UtcNow;

            // 2. Obtener (o crear) la cola de timestamps para esta IP
            var requests = _requestTracker.GetOrAdd(ip, _ => new ConcurrentQueue<DateTime>());

            // 3. Eliminar timestamps que ya están fuera de la ventana de tiempo
            while (requests.TryPeek(out DateTime time) && (now - time) > _window)
            {
                requests.TryDequeue(out _);
            }

            // 4. ¿Supera el límite?
            if (requests.Count >= _limit)
            {
                // Log a FICHERO, solo 1 vez por IP por ventana (evita saturar el log en ataques)
                bool debeLoguear = !_lastLoggedBlock.TryGetValue(ip, out DateTime ultimoLog)
                                   || (now - ultimoLog) > _window;
                if (debeLoguear)
                {
                    _lastLoggedBlock[ip] = now;
                    LogHelper.Log(
                        $"RATE LIMIT EXCEDIDO: IP={ip} | Límite={_limit} req/{_window.TotalSeconds}s | " +
                        $"Endpoint={actionContext.ActionDescriptor?.ActionName}",
                        "WARN");
                }

                // HTTP 429 Too Many Requests (estándar correcto — el enum antiguo no lo incluye directamente)
                actionContext.Response = actionContext.Request.CreateResponse(
                    (HttpStatusCode)429,
                    new { error = "Too Many Requests", message = $"Límite de {_limit} peticiones por {_window.TotalSeconds}s superado. Intenta de nuevo más tarde." });
                return;
            }

            // 5. Petición dentro del límite → registrar y continuar
            requests.Enqueue(now);
            base.OnActionExecuting(actionContext);
        }

        /// <summary>
        /// Elimina de memoria las IPs que llevan más de 10 minutos sin actividad.
        /// Se ejecuta cada 5 minutos para no impactar el rendimiento.
        /// </summary>
        private void LimpiarIPsInactivas()
        {
            var now = DateTime.UtcNow;
            if ((now - _lastCleanup) < _cleanupInterval) return;

            lock (_cleanupLock)
            {
                // Double-check tras adquirir el lock (patrón correcto para thread-safety)
                if ((now - _lastCleanup) < _cleanupInterval) return;
                _lastCleanup = now;

                var cutoff = now.AddMinutes(-10);
                foreach (var key in _requestTracker.Keys.ToList())
                {
                    if (_requestTracker.TryGetValue(key, out var queue) &&
                        (queue.IsEmpty || (queue.TryPeek(out DateTime oldest) && oldest < cutoff)))
                    {
                        _requestTracker.TryRemove(key, out _);
                        _lastLoggedBlock.TryRemove(key, out _);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Atributo para monitoreo de seguridad y registro de accesos.
    /// </summary>
    public class SecurityMonitoringAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            string ip = SecurityService.GetClientIp(actionContext.Request);

            // 1. Verificación de BlackList (Lista Negra)
            if (SecurityService.IsIpBlocked(ip))
            {
                LogHelper.Warn($"ACCESO DENEGADO (BlackList): IP {ip} intentó acceder a {actionContext.ActionDescriptor.ControllerDescriptor.ControllerName}/{actionContext.ActionDescriptor.ActionName}");
                
                actionContext.Response = actionContext.Request.CreateResponse(
                    System.Net.HttpStatusCode.Forbidden, 
                    new { error = "Access Denied", message = "Su dirección IP ha sido bloqueada por políticas de seguridad." }
                );
                return;
            }

            // 2. Loguear acceso (Traza de seguridad nivel INFO)
            // LogHelper.Info($"Access Attempt: {actionContext.ActionDescriptor.ActionName} from IP {ip}");
            
            base.OnActionExecuting(actionContext);
        }
    }

    /// <summary>
    /// Valida que el ID en la URL sea un Base64 válido
    /// </summary>
    public class ValidateEncryptedIdAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            // Buscamos el parámetro "id" que es el que usa el controlador
            if (actionContext.ActionArguments.ContainsKey("id"))
            {
                var id = actionContext.ActionArguments["id"] as string;
                if (string.IsNullOrEmpty(id) || !IsBase64(id))
                {
                     LogHelper.Warn($"Intento de acceso con ID no válido (No es Base64): {id}");
                     actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "El ID proporcionado no tiene un formato válido.");
                }
            }
            base.OnActionExecuting(actionContext);
        }

        private bool IsBase64(string base64String)
        {
            if (string.IsNullOrEmpty(base64String)) return false;

            // 1. Limpieza básica de espacios (evitar falsos negativos por espacios en blanco accidentales)
            string s = base64String.Trim().Replace(" ", "+");

            // 2. Normalizar Base64URL -> Base64 Estándar
            s = s.Replace("-", "+").Replace("_", "/");

            // 3. Añadir padding si falta (Convert.FromBase64String lo requiere estrictamente)
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "="; break;
            }

            try
            {
                Convert.FromBase64String(s);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Requiere API Key en el Header
    /// </summary>
    //public class ApiKeyAuthAttribute : ActionFilterAttribute
    //{
    //    public override void OnActionExecuting(HttpActionContext actionContext)
    //    {
    //        if (!actionContext.Request.Headers.Contains("X-API-Key"))
    //        {
    //            actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized, "API Key Missing");
    //            return;
    //        }
    //        base.OnActionExecuting(actionContext);
    //    }
    //}

    public class ApiKeyAuthAttribute : ActionFilterAttribute
    {
        private const string HeaderName = "X-API-Key";

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            // 1. Verificar si existe la cabecera
            if (!actionContext.Request.Headers.Contains(HeaderName))
            {
                actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized, "API Key Missing");
                return;
            }

            // 2. Obtener el valor de la cabecera
            var incomingKey = actionContext.Request.Headers.GetValues(HeaderName).FirstOrDefault();

            // 3. Obtener el valor esperado del Web.config
            var expectedKey = ConfigurationManager.AppSettings["ApiKey"];

            // 4. Comparar (Safe compare para evitar timing attacks)
            if (string.IsNullOrEmpty(expectedKey) || !RecordatorioEnvio.Infrastructure.Security.SecurityService.ConstantTimeEquals(incomingKey, expectedKey))
            {
                actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized, "Invalid API Key");
                return;
            }

            base.OnActionExecuting(actionContext);
        }
    }
}
