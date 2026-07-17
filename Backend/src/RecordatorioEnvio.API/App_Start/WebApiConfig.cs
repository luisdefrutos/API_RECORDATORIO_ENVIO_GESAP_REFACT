using System.Web.Http;
using System.Web.Http.Cors;
using Newtonsoft.Json.Serialization;
using RecordatorioEnvio.API.App_Start;
using RecordatorioEnvio.API.Filters;

namespace RecordatorioEnvio.API
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Configurar DI
            config.DependencyResolver = new SimpleDependencyResolver();

            // Web API configuration and services
            // Orígenes permitidos leídos de config. Sin comodín: si no está definido, NO se habilita CORS.
            string corsOrigins = System.Configuration.ConfigurationManager.AppSettings["CorsOrigins"];
            if (!string.IsNullOrWhiteSpace(corsOrigins) && corsOrigins.Trim() != "*")
            {
                // Usamos una política dinámica para permitir localhost en cualquier puerto (Desarrollo)
                // y los servidores oficiales (Producción/Preproducción) validados desde config.
                var cors = new RecordatorioEnvio.API.App_Start.DynamicCorsPolicyAttribute(corsOrigins);
                config.EnableCors(cors);
            }
            // Si no hay CorsOrigins (o es "*") → no se registra CORS (deny by default).

            // --- SEGURIDAD GLOBAL ---
            // 1. Rate Limiting (Configurable desde Web.config)
            int rlLimit = int.TryParse(System.Configuration.ConfigurationManager.AppSettings["RateLimit_Limit"], out int l) ? l : 500;
            int rlWindow = int.TryParse(System.Configuration.ConfigurationManager.AppSettings["RateLimit_WindowSeconds"], out int w) ? w : 60;
            
            config.Filters.Add(new RateLimitAttribute(limit: rlLimit, windowSeconds: rlWindow)); 
            
            // 2. Autenticación por API Key (X-API-Key header)
            config.Filters.Add(new ApiKeyAuthAttribute());

            // 3. Monitoreo de Seguridad (Auditoría)
            config.Filters.Add(new SecurityMonitoringAttribute());
            // -------------------------
            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // JSON CamelCase
            var json = config.Formatters.JsonFormatter;
            json.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            config.Formatters.Remove(config.Formatters.XmlFormatter);
        }
    }
}
