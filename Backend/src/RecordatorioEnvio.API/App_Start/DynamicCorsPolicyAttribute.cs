using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Cors;
using System.Web.Http.Cors;

namespace RecordatorioEnvio.API.App_Start
{
    /// <summary>
    /// Proveedor de políticas CORS dinámico para permitir cualquier puerto de localhost en desarrollo,
    /// y leer de la configuración (Web.config) para los entornos de Producción/Preproducción.
    /// Esto evita el uso del comodín global (*) y cumple con las normativas de Ciberseguridad.
    /// </summary>
    public class DynamicCorsPolicyAttribute : Attribute, ICorsPolicyProvider
    {
        private readonly string _allowedOrigins;

        public DynamicCorsPolicyAttribute(string allowedOrigins)
        {
            _allowedOrigins = allowedOrigins;
        }

        public Task<CorsPolicy> GetCorsPolicyAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var policy = new CorsPolicy
            {
                AllowAnyMethod = true,
                AllowAnyHeader = true
            };

            // Intentar leer la cabecera "Origin" de la petición
            if (request.Headers.Contains("Origin"))
            {
                var origin = request.Headers.GetValues("Origin").FirstOrDefault();
                if (!string.IsNullOrEmpty(origin))
                {
                    // 1. REGLA DESARROLLADORES: Permitir localhost en CUALQUIER puerto
                    if (origin.StartsWith("http://localhost", StringComparison.OrdinalIgnoreCase) || 
                        origin.StartsWith("https://localhost", StringComparison.OrdinalIgnoreCase))
                    {
                        policy.Origins.Add(origin);
                        return Task.FromResult(policy);
                    }

                    // 2. REGLA SERVIDORES: Comprobar la lista de orígenes permitidos en Web.config
                    if (!string.IsNullOrWhiteSpace(_allowedOrigins))
                    {
                        var allowedList = _allowedOrigins.Split(',').Select(o => o.Trim().TrimEnd('/'));
                        var cleanOrigin = origin.TrimEnd('/');
                        
                        if (allowedList.Contains(cleanOrigin, StringComparer.OrdinalIgnoreCase))
                        {
                            policy.Origins.Add(origin);
                            return Task.FromResult(policy);
                        }
                    }
                }
            }

            // Si no coincide ninguna regla, se devuelve una política vacía (bloqueo CORS por defecto)
            return Task.FromResult(policy);
        }
    }
}
