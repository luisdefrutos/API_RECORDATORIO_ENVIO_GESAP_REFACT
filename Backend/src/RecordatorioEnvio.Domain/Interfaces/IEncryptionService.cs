namespace RecordatorioEnvio.Domain.Interfaces
{
    /// <summary>
    /// Abstracción del servicio de cifrado/descifrado.
    /// Permite sustituir la implementación concreta (AES-256) sin modificar
    /// los consumidores (RecordatorioService, ProxyController).
    /// Principio: Dependency Inversion (DIP).
    /// </summary>
    public interface IEncryptionService
    {
        /// <summary>Cifra un texto plano y devuelve un token Base64-URL seguro.</summary>
        string Encrypt(string plainText);

        /// <summary>Descifra un token generado por Encrypt. Devuelve null si es inválido o ha sido manipulado.</summary>
        string Decrypt(string token);
    }
}
