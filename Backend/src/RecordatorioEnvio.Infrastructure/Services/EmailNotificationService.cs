using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using RecordatorioEnvio.Domain.Interfaces;

namespace RecordatorioEnvio.Infrastructure.Services
{
    public class EmailNotificationService : IEmailNotificationService
    {
        private readonly ISysConfiguracionRepository _configRepo;

        public EmailNotificationService(ISysConfiguracionRepository configRepo)
        {
            _configRepo = configRepo;
        }

        public async Task<bool> SendEmailAsync(string destinatario, string asunto, string cuerpoHtml)
        {
            var config = _configRepo.GetConfiguracion();
            if (config == null) throw new Exception("No se encontró configuración en SYS_CONFIGURACION");

            RecordatorioEnvio.Infrastructure.Logging.LogHelper.Log($"[EMAIL] Config DB obtenida -> Host: {config.ServidorCorreoDireccion}, Puerto: {config.ServidorCorreoPuerto}", "INFO");

            if (!String.IsNullOrEmpty(destinatario))
            {
                var correosBrutos = destinatario.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                var correosValidos = new System.Collections.Generic.List<string>();

                foreach (var correo in correosBrutos)
                {
                    string girTrimmed = correo.Trim();
                    bool isGirRegexValid = System.Text.RegularExpressions.Regex.IsMatch(girTrimmed, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

                    if (isGirRegexValid)
                    {
                        correosValidos.Add(girTrimmed); // Añadimos a la lista limpia
                    }
                    else
                    {
                        RecordatorioEnvio.Infrastructure.Logging.LogHelper.Log($"[EMAIL] Un correo ({girTrimmed}) no supera validación Regex. Ignorado.", "WARN");
                    }
                }

                // Sobrescribimos el original uniendo la lista limpia con puntos y comas
                destinatario = String.Join(";", correosValidos);
            }

            return await EnviarEmailAsync(
                config.EmailNotificacionesUsuario,
                config.EmailNotificacionesPassword,
                config.ServidorCorreoDireccion,
                config.ServidorCorreoPuerto,
                config.EmailNotificacionesDireccion,
                "Notificación TÜV SÜD",
                config.EmailNotificacionesDireccion,
                "Notificación TÜV SÜD",
                asunto,
                cuerpoHtml,
                destinatario?.Trim().ToLower() ?? "",
                config.SslHabilitado
            );
        }

        private async Task<bool> EnviarEmailAsync(
            string usuario, string password, string servidor, string puertoStr,
            string emailOrigen, string nombreOrigen,
            string emailRespuesta, string nombreRespuesta,
            string asunto, string cuerpo, string emailDestinatario, bool habilitarSsl)
        {
            if (string.IsNullOrWhiteSpace(emailOrigen) ||
                string.IsNullOrWhiteSpace(emailRespuesta) ||
                string.IsNullOrWhiteSpace(emailDestinatario))
            {
                throw new ArgumentException("Parámetros origen, respuesta o destinatario vacíos");
            }

            if (!int.TryParse(puertoStr, out int puerto))
            {
                puerto = 25; // Puerto SMTP por defecto si falla el parseo
            }

            using (var email = new MailMessage())
            {
                var destinatarios = emailDestinatario.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var dest in destinatarios)
                {
                    if (!string.IsNullOrWhiteSpace(dest))
                    {
                        string trimmed = dest.Trim();
                        // Nueva validación Regex doble comprobación
                        bool isRegexValid = System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                        
                        if (!isRegexValid)
                        {
                            RecordatorioEnvio.Infrastructure.Logging.LogHelper.Log($"[EMAIL] Se ignoró una dirección de correo por no cumplir validación Regex: {trimmed}", "WARNING");
                            continue;
                        }

                        try
                        {
                            var addr = new System.Net.Mail.MailAddress(trimmed);
                            email.To.Add(addr);
                        }
                        catch
                        {
                            RecordatorioEnvio.Infrastructure.Logging.LogHelper.Log($"[EMAIL] Se ignoró una dirección de correo con formato inválido (.NET): {trimmed}", "WARNING");
                        }
                    }
                }

                if (email.To.Count == 0)
                {
                    RecordatorioEnvio.Infrastructure.Logging.LogHelper.Log("[EMAIL] No se pudo enviar el correo porque no hay destinatarios válidos.", "ERROR");
                    return false;
                }

                email.From = new MailAddress(emailOrigen, nombreOrigen);
                email.ReplyToList.Add(new MailAddress(emailRespuesta, nombreRespuesta));
                email.Subject = asunto.Length > 100 ? asunto.Substring(0, 100) : asunto;
                email.Body = cuerpo;
                email.IsBodyHtml = true;

                // --- INICIO CÓDIGO LOGO EMBEBIDO ---
                // Embed the logo to prevent Outlook from blocking the external URL
                if (cuerpo.Contains("cid:logotuv"))
                {
                    var htmlView = System.Net.Mail.AlternateView.CreateAlternateViewFromString(cuerpo, null, "text/html");
                    string b64Logo = "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";
                    var ms = new System.IO.MemoryStream(Convert.FromBase64String(b64Logo));
                    var lr = new System.Net.Mail.LinkedResource(ms, "image/png");
                    lr.ContentId = "logotuv";
                    htmlView.LinkedResources.Add(lr);
                    email.AlternateViews.Add(htmlView);
                }
                // --- FIN CÓDIGO LOGO EMBEBIDO ---


                using (var client = new SmtpClient())
                {
                    if (!string.IsNullOrEmpty(usuario) && !string.IsNullOrEmpty(password))
                    {
                        client.Credentials = new NetworkCredential(usuario, password);
                    }
                    client.Port = puerto;
                    client.Host = servidor;
                    client.EnableSsl = true; // Forzado a True porque el servidor (ej. Office 365) exige STARTTLS (Error 5.7.0)
                    client.Timeout = 10000; // 10 segundos de timeout en lugar de 100 (evita 500 en Proxy)

                    try 
                    {
                        await client.SendMailAsync(email);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        RecordatorioEnvio.Infrastructure.Logging.LogHelper.Error(ex, $"Fallo SmtpClient. Host: {servidor}:{puerto}, SSL: {habilitarSsl}, From: {emailOrigen}, To: {emailDestinatario}");
                        throw;
                    }
                }
            }
        }
    }
}
