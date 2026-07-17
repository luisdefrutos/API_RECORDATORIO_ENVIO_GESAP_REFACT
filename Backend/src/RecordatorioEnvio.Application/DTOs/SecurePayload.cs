namespace RecordatorioEnvio.Application.DTOs
{
    /// <summary>
    /// Envoltorio para transportar payloads cifrados (AES-256 + HMAC).
    /// </summary>
    public class SecurePayload
    {
        public string Data { get; set; }
    }
}
