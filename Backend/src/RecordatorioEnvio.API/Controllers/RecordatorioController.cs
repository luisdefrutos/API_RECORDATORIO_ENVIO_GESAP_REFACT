using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using RecordatorioEnvio.Application.DTOs;
using RecordatorioEnvio.Application.Services;
using RecordatorioEnvio.Infrastructure.Logging;
using RecordatorioEnvio.Domain.Interfaces;
using RecordatorioEnvio.API.Filters;
using Newtonsoft.Json;
using System.Linq;

namespace RecordatorioEnvio.API.Controllers
{
    /// <summary>
    /// de
    /// </summary>
    [RoutePrefix("api/recordatorio")]
    public class RecordatorioController : ApiController
    {
        private readonly RecordatorioService   _service;
        private readonly IEncryptionService     _encryptionService; // DIP: abstracción
        private readonly IEmailNotificationService _emailService;

        public RecordatorioController(RecordatorioService service, IEncryptionService encryptionService, IEmailNotificationService emailService)
        {
            _service = service;
            _encryptionService = encryptionService;
            _emailService = emailService;
        }

        private string GetCorrId() => Request.Headers.Contains("X-Correlation-ID") 
            ? System.Linq.Enumerable.First(Request.Headers.GetValues("X-Correlation-ID")) 
            : "N/A";

        /// <summary>
        /// Genera una respuesta 500 estándar ocultando detalles internos.
        /// Constante centralizada para facilitar modificaciones futuras.
        /// </summary>
        private IHttpActionResult SafeInternalServerError()
        {
            const string MensajeErrorGenerico = "Ocurrió un error inesperado al procesar su solicitud. El incidente ha sido registrado.";
            return Content(HttpStatusCode.InternalServerError, new { message = MensajeErrorGenerico });
        }

        /// <summary>
        /// Obtiene datos por ID (Versión Segura)
        /// 
        /// Capas de seguridad aplicadas:
        /// - [Global] ApiKeyAuth: Requiere API Key en header X-API-Key
        /// - [Global] SecurityMonitoring: Auditoría de acceso
        /// - ValidateEncryptedId: Valida formato del token Base64
        /// - Encriptación: ID recibido es un token cifrado (AES-256 + HMAC)
        /// </summary>
        /// <param name="id">Token encriptado del registro</param>
        /// <returns>Datos del registro en formato JSON</returns>
        [ValidateEncryptedId]
        [HttpGet]
        [Route("{id}")]
        public IHttpActionResult Get(string id)
        {
            try
            {
                // 1. Desencriptar ID
                var decryptedIdStr = _encryptionService.Decrypt(id);
                if (string.IsNullOrEmpty(decryptedIdStr) || !long.TryParse(decryptedIdStr, out long decryptedId))
                {
                    LogHelper.Log($"Intento de acceso con ID inválido o manipulado: {id}", "WARN", corrId: GetCorrId());
                    return BadRequest("ID inválido"); 
                }

                LogHelper.Info($"Acceso concedido a registro", corrId: GetCorrId(), recordId: decryptedId);

                var result = _service.GetByIdRecordatorioEnvio(decryptedId);
                if (result == null)
                {
                    return NotFound();
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Loguear excepción
                LogHelper.Error(ex, $"GET ID {id}");
                return SafeInternalServerError();
            }
        }

        // Helper para generar tokens de prueba (SOLO ERROR DEBUG)
        // Permite al desarrollador cifrar un ID para probar el flujo completo
        // Helper para generar tokens de prueba (SOLO ERROR DEBUG)
        // Permite al desarrollador cifrar un ID para probar el flujo completo
        [HttpGet]
        [Route("encrypt/{id:long}")]
        public IHttpActionResult GetEncrypt(long id)
        {
            var esDesarrolloStr = System.Configuration.ConfigurationManager.AppSettings["EsDesarrollo"];
            bool esDesarrollo = false;
            if (!string.IsNullOrEmpty(esDesarrolloStr) && bool.TryParse(esDesarrolloStr, out bool parsed))
            {
                esDesarrollo = parsed;
            }

            if (!esDesarrollo)
            {
                return StatusCode(HttpStatusCode.NoContent);
            }

            var token = _encryptionService.Encrypt(id.ToString());
            // Retorna JSON con el token y el link directo
            return Ok(new { 
                Id = id, 
                Token = token, 
                Link = $"/Proxy/GetRecordatorio?id={token}" 
            });

        }

        [HttpGet]
        [Route("estados")]
        public IHttpActionResult GetEstados()
        {
            try
            {
                var result = _service.GetTodosEstados();
                return Ok(result);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex, "GET ESTADOS");
                return SafeInternalServerError();
            }
        }

        [HttpPost]
        [Route("")]
        public async Task<IHttpActionResult> Post([FromBody] RecordatorioEnvioRespDto dto)
        {
            if (dto == null) 
                return BadRequest("Payload vacío o inválido");

            try
            {
                // --- PROTECCIÓN IDOR (TOKEN DE INMUTABILIDAD) ---
                // Verifica que el ID que viene en el DTO es realmente el ID sobre el que el usuario tiene permisos
                string inmutabilityTokenHeader = null;
                if (Request.Headers.TryGetValues("X-Inmutability-Token", out var values))
                {
                    inmutabilityTokenHeader = values.FirstOrDefault();
                }

                if (string.IsNullOrEmpty(inmutabilityTokenHeader))
                {
                    LogHelper.Log("[SEGURIDAD] Petición POST rechazada: Falta token de inmutabilidad.", "FATAL", corrId: GetCorrId());
                    return BadRequest("Violación de seguridad detectada: Falta token inmutable.");
                }

                string decryptedIdStr = _encryptionService.Decrypt(inmutabilityTokenHeader);
                if (!long.TryParse(decryptedIdStr, out long idRealCifrado) || dto.IdRecordatorioEnvio != idRealCifrado)
                {
                    LogHelper.Log($"[SEGURIDAD] Intento de manipulación de ID detectado (IDOR). DTO: {dto.IdRecordatorioEnvio}, Token: {decryptedIdStr}", "FATAL", corrId: GetCorrId());
                    return BadRequest("Violación de seguridad detectada: Identificador inmutable alterado.");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex, "POST - Error de Seguridad / Validación de IDOR", corrId: GetCorrId());
                return SafeInternalServerError();
            }

            try
            {
                // 3. Validaciones de Negocio (Backend)
                var validationErrors = _service.ValidateRules(dto);
                if (validationErrors != null && validationErrors.Count > 0)
                {
                    return Content(HttpStatusCode.BadRequest, new { success = false, message = "Errores de validación", errors = validationErrors });
                }

                // 4. Guardado en Base de Datos (Commit)
                _service.Update(dto);

                // Recuperar la entidad COMPLETA desde BBDD para montar el email, 
                // asegurando que tenemos IdentificadorRecEnvio, Detalles, etc.
                // aunque el frontend no los haya enviado en el payload del POST.
                var dtoCompletoParaEmail = _service.GetByIdRecordatorioEnvio(dto.IdRecordatorioEnvio) ?? dto;

                // 5. Envío de Correo Síncrono
                bool correoEnviado = false;
                string emailsDestino = ""; // Variable para guardar a quién se lo mandamos realmente
                string originalEmailsDestino = ""; // Variable para guardar correos reales sin hardcodear

                try
                {
                    // Decidir a quién le mandamos el correo dependiendo de a quién se factura
                    emailsDestino = dtoCompletoParaEmail.FacturarA == "GESTOR" 
                        ? dtoCompletoParaEmail.RepresentanteEmail 
                        : dtoCompletoParaEmail.TitularEmail;

                    // Fallback de seguridad: la web siempre obliga a poner el del Titular, 
                    // si por algún casual la lógica anterior falla, usamos el del titular.
                    if (string.IsNullOrWhiteSpace(emailsDestino) && !string.IsNullOrWhiteSpace(dtoCompletoParaEmail.TitularEmail))
                    {
                        emailsDestino = dtoCompletoParaEmail.TitularEmail;
                    }

                    // Añadir el CentroEmail si existe en el primer detalle (o cualquier detalle válido)
                    var primerCentroEmail = dtoCompletoParaEmail.Detalles?.FirstOrDefault(d => !string.IsNullOrWhiteSpace(d.CentroEmail))?.CentroEmail;
                    
                    if (!string.IsNullOrWhiteSpace(primerCentroEmail))
                    {
                        if (!string.IsNullOrWhiteSpace(emailsDestino))
                            emailsDestino += ";" + primerCentroEmail.Trim();
                        else
                            emailsDestino = primerCentroEmail.Trim();
                    }

                    // --- VALIDACIÓN REGEX ---
                    if (!string.IsNullOrWhiteSpace(emailsDestino))
                    {
                        var correosArray = emailsDestino.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                        var correosValidos = new System.Collections.Generic.List<string>();
                        
                        foreach (var c in correosArray)
                        {
                            var emailTrimmed = c.Trim();
                            if (System.Text.RegularExpressions.Regex.IsMatch(emailTrimmed, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                            {
                                correosValidos.Add(emailTrimmed);
                            }
                            else
                            {
                                LogHelper.Log($"POST - El correo proporcionado ({emailTrimmed}) no supera la validación Regex. Se omite.", "WARN", corrId: GetCorrId());
                            }
                        }
                        
                        emailsDestino = string.Join(";", correosValidos);
                    }

                    originalEmailsDestino = emailsDestino;

                    if (!string.IsNullOrWhiteSpace(emailsDestino))
                    {
                        // Hardcodeo temporal para pruebas en desarrollo
                        if (System.Configuration.ConfigurationManager.AppSettings["EsDesarrollo"]?.ToLower() == "true")
                        {
                            
                            emailsDestino = "luis.defrutos@tuvsud.com;pablo.martin@tuvsud.com;pedro.pacheco@tuvsud.com";
                            LogHelper.Log("POST - [MODO DESARROLLO] Correos sobrescritos por los de prueba.", "INFO", corrId: GetCorrId());
                        }

                        string asunto = $"Confirmación de su oferta : {dtoCompletoParaEmail.IdentificadorRecEnvio}";
                        string cuerpoHtml = RecordatorioEnvio.Application.Services.EmailTemplateBuilder.ConstruirCuerpoEmail(dtoCompletoParaEmail);
                        
                        correoEnviado = await _emailService.SendEmailAsync(emailsDestino, asunto, cuerpoHtml);
                    }
                    else
                    {
                        LogHelper.Log("POST - No hay correo de destino disponible para enviar notificación.", "WARN", corrId: GetCorrId());
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Error(ex, "POST - Fallo al enviar correo tras guardado exitoso", corrId: GetCorrId());
                }

                // 6. Construir respuesta final
                string emailsFormateados = emailsDestino?.ToLower().Replace(";", "<br>") ?? "nuestro sistema";
                string finalMessage = correoEnviado 
                    ? $"Datos guardados y notificación enviada a:<br><strong style='color:#004a80;'>{emailsFormateados}</strong>"
                    : "Datos guardados correctamente (Advertencia: No se pudo enviar el correo)";

                return Ok(new { success = true, message = finalMessage });
            }
            catch (ArgumentException ax)
            {
                 LogHelper.Error(ax, "POST - Validación");
                 return BadRequest(ax.Message);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex, "POST - Excepción General");
                return SafeInternalServerError();
            }
        }

        [HttpGet]
        [Route("debug/check-ip")]
        public IHttpActionResult CheckIp(string testIp)
        {
            var esDesarrolloStr = System.Configuration.ConfigurationManager.AppSettings["EsDesarrollo"];
            if (string.IsNullOrEmpty(esDesarrolloStr) || !esDesarrolloStr.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }

            try
            {
                bool isBlocked = RecordatorioEnvio.Infrastructure.Security.SecurityService.IsIpBlocked(testIp);
                string rules = System.Configuration.ConfigurationManager.AppSettings["Security_BlackList"] ?? "(Ninguna)";
                return Ok(new { blocked = isBlocked, rules = rules });
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex, "DEBUG CHECK IP");
                return SafeInternalServerError();
            }

        }

        [HttpGet]
        [Route("debug/logs")]
        public IHttpActionResult GetLogs()
        {
            var esDesarrolloStr = System.Configuration.ConfigurationManager.AppSettings["EsDesarrollo"];
            if (string.IsNullOrEmpty(esDesarrolloStr) || !esDesarrolloStr.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }

            try
            {
                var logs = _service.GetRecentLogs(10);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex, "DEBUG GET LOGS");
                return SafeInternalServerError();
            }

        }
    }
}
