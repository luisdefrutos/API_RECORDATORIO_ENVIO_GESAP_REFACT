using System.Threading.Tasks;

namespace RecordatorioEnvio.Domain.Interfaces
{
    public interface IEmailNotificationService
    {
        Task<bool> SendEmailAsync(string destinatario, string asunto, string cuerpoHtml);
    }
}
