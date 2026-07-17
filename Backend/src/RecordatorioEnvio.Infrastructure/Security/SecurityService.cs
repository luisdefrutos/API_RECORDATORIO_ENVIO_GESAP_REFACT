using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Web;

namespace RecordatorioEnvio.Infrastructure.Security
{
    /// <summary>
    /// Servicio para gestionar la seguridad de acceso por IP.
    /// </summary>
    public static class SecurityService
    {
        private static HashSet<string> _jsonBlackList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static DateTime _lastJsonCheck = DateTime.MinValue;
        private static readonly object _syncLock = new object();

        /// <summary>
        /// Comprueba si una dirección IP está bloqueada basándose en la lista negra del Web.config y un archivo JSON opcional.
        /// </summary>
        /// <param name="userIp">Dirección IP del cliente.</param>
        /// <returns>True si la IP coincide con alguna regla de bloqueo.</returns>
        public static bool IsIpBlocked(string userIp)
        {
            if (string.IsNullOrEmpty(userIp)) return false;

            // 1. Verificación en Web.config (Prioridad para reglas rápidas y patrones)
            string blackListConfig = ConfigurationManager.AppSettings["Security_BlackList"];
            if (!string.IsNullOrEmpty(blackListConfig))
            {
                string[] rules = blackListConfig.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                               .Select(r => r.Trim())
                                               .ToArray();

                foreach (var rule in rules)
                {
                    if (MatchesRule(userIp, rule)) return true;
                }
            }

            // 2. Verificación en JSON (Para listados extensos de IPs exactas)
            if (IsIpInJsonBlackList(userIp)) return true;

            return false;
        }

        /// <summary>
        /// Carga y verifica la IP contra el archivo JSON con sistema de caché.
        /// </summary>
        private static bool IsIpInJsonBlackList(string userIp)
        {
            string jsonPath = ConfigurationManager.AppSettings["Security_BlackList_JsonPath"];
            if (string.IsNullOrEmpty(jsonPath)) return false;

            try
            {
                // Mapear ruta virtual a física (ej: ~/App_Data/blacklist.json)
                string physicalPath = jsonPath.StartsWith("~") 
                    ? System.Web.Hosting.HostingEnvironment.MapPath(jsonPath) 
                    : jsonPath;

                if (!System.IO.File.Exists(physicalPath)) return false;

                DateTime lastWrite = System.IO.File.GetLastWriteTimeUtc(physicalPath);

                // Recargar solo si el archivo ha cambiado
                if (lastWrite > _lastJsonCheck)
                {
                    lock (_syncLock)
                    {
                        if (lastWrite > _lastJsonCheck)
                        {
                            string json = System.IO.File.ReadAllText(physicalPath);
                            var list = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json);
                            _jsonBlackList = new HashSet<string>(list ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
                            _lastJsonCheck = lastWrite;
                        }
                    }
                }

                return _jsonBlackList.Contains(userIp);
            }
            catch (Exception ex)
            {
                // No bloqueamos el servicio por un error de lectura, pero lo logueamos
                System.Diagnostics.Debug.WriteLine($"Error reading JSON Blacklist: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Lógica de matching para IPs. Soporta exactitud y comodines finales (ej: 192.168.1.*).
        /// </summary>
        private static bool MatchesRule(string ip, string rule)
        {
            // Caso 1: Regla es un comodín total (No recomendado pero soportado)
            if (rule == "*") return true;

            // Caso 2: Comodín al final (Rango de subred)
            if (rule.EndsWith("*"))
            {
                string prefix = rule.Substring(0, rule.Length - 1);
                // Si el prefijo no termina en punto, lo añadimos para evitar que "10.*" bloquee "100.1.1.1"
                if (!prefix.EndsWith(".") && prefix.Length > 0 && !ip.Equals(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return ip.StartsWith(prefix + ".", StringComparison.OrdinalIgnoreCase);
                }
                return ip.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
            }

            // Caso 3: IP Exacta
            return ip.Equals(rule, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Obtiene la IP real del cliente, considerando posibles proxies o balanceadores.
        /// Versión para ASP.NET MVC (HttpRequestBase)
        /// </summary>
        public static string GetClientIp(System.Web.HttpRequestBase request)
        {
            if (request == null) return "unknown";
            
            string physicalIp = request.UserHostAddress;
            if (IsPrivateOrLocalIp(physicalIp))
            {
                string forwarded = request.Headers["X-Forwarded-For"];
                if (!string.IsNullOrEmpty(forwarded)) return forwarded.Split(',')[0].Trim();
                
                string incap = request.Headers["Incap-Client-IP"];
                if (!string.IsNullOrEmpty(incap)) return incap.Trim();
            }

            return physicalIp;
        }

        /// <summary>
        /// Versión para ASP.NET Tradicional (HttpRequest)
        /// </summary>
        public static string GetClientIp(System.Web.HttpRequest request)
        {
            if (request == null) return "unknown";
            
            string physicalIp = request.UserHostAddress;
            if (IsPrivateOrLocalIp(physicalIp))
            {
                string forwarded = request.Headers["X-Forwarded-For"];
                if (!string.IsNullOrEmpty(forwarded)) return forwarded.Split(',')[0].Trim();
                
                string incap = request.Headers["Incap-Client-IP"];
                if (!string.IsNullOrEmpty(incap)) return incap.Trim();
            }

            return physicalIp;
        }

        /// <summary>
        /// Versión para WebAPI (HttpRequestMessage)
        /// </summary>
        public static string GetClientIp(System.Net.Http.HttpRequestMessage request)
        {
            if (request == null) return "unknown";

            // 3. Intentar obtener de HttpContext (IIS)
            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                var ctx = request.Properties["MS_HttpContext"] as System.Web.HttpContextBase;
                if (ctx != null)
                {
                    return GetClientIp(ctx.Request);
                }
            }

            // 4. Intentar obtener de RemoteEndpoint (Self-host / Owin)
            if (request.Properties.ContainsKey("System.ServiceModel.Channels.RemoteEndpointMessageProperty"))
            {
                dynamic prop = request.Properties["System.ServiceModel.Channels.RemoteEndpointMessageProperty"];
                return prop.Address;
            }

            return "unknown";
        }

        /// <summary>
        /// Comprueba si una IP pertenece a una red local o privada (Clases A, B, C).
        /// </summary>
        public static bool IsPrivateOrLocalIp(string ipAddress)
        {
            if (string.IsNullOrEmpty(ipAddress)) return false;
            if (ipAddress == "::1" || ipAddress == "127.0.0.1") return true;

            if (System.Net.IPAddress.TryParse(ipAddress, out var ip))
            {
                byte[] bytes = ip.GetAddressBytes();
                if (bytes.Length == 4) // IPv4
                {
                    // Clase A: 10.0.0.0 - 10.255.255.255
                    if (bytes[0] == 10) return true;
                    // Clase B: 172.16.0.0 - 172.31.255.255
                    if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
                    // Clase C: 192.168.0.0 - 192.168.255.255
                    if (bytes[0] == 192 && bytes[1] == 168) return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Compara dos cadenas de texto de forma segura previniendo ataques de timing.
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining | System.Runtime.CompilerServices.MethodImplOptions.NoOptimization)]
        public static bool ConstantTimeEquals(string a, string b)
        {
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;

            int diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }
            return diff == 0;
        }

        /// <summary>
        /// Compara dos arrays de bytes de forma segura previniendo ataques de timing.
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining | System.Runtime.CompilerServices.MethodImplOptions.NoOptimization)]
        public static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;

            int diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }
            return diff == 0;
        }
    }
}
