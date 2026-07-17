using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using RecordatorioEnvio.Domain.Interfaces;

namespace RecordatorioEnvio.Infrastructure.Encryption
{
    public class EncryptionService : IEncryptionService
    {
        private readonly byte[] _key;
        private readonly byte[] _hmacKey;

        public EncryptionService()
        {
            var keyBase64 = ConfigurationManager.AppSettings["EncryptionKey"];
            var hmacKeyBase64 = ConfigurationManager.AppSettings["HmacKey"];

            if (string.IsNullOrEmpty(keyBase64) || string.IsNullOrEmpty(hmacKeyBase64))
            {
                throw new ConfigurationErrorsException("EncryptionKey and HmacKey must be configured in Web.config");
            }

            try 
            {
                // Keys are expected to be 32 bytes (AES-256)
                _key = Convert.FromBase64String(keyBase64.Trim());
                _hmacKey = Convert.FromBase64String(hmacKeyBase64.Trim());

                if (_key.Length != 32) throw new ConfigurationErrorsException($"EncryptionKey must be 32 bytes (Current: {_key.Length})");
                if (_hmacKey.Length != 32) throw new ConfigurationErrorsException($"HmacKey must be 32 bytes (Current: {_hmacKey.Length})");
            }
            catch (Exception ex)
            {
                throw new ConfigurationErrorsException("Error initializing encryption keys.", ex);
            }
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return null;

            using (var aes = Aes.Create())
            {
                // FORCE 256 bits (32 bytes)
                aes.KeySize = 256;
                aes.Key = _key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                
                // Generate Random IV
                aes.GenerateIV();
                byte[] iv = aes.IV;

                byte[] encryptedContent;
                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs))
                    {
                        sw.Write(plainText);
                    }
                    encryptedContent = ms.ToArray();
                }

                // Payload = IV (16) + EncryptedContent (N)
                var cipherBytes = new byte[iv.Length + encryptedContent.Length];
                Buffer.BlockCopy(iv, 0, cipherBytes, 0, iv.Length);
                Buffer.BlockCopy(encryptedContent, 0, cipherBytes, iv.Length, encryptedContent.Length);

                // Compute HMAC of (IV + EncryptedContent)
                using (var hmac = new HMACSHA256(_hmacKey))
                {
                    var hmacBytes = hmac.ComputeHash(cipherBytes);
                    
                    // Final Token = CipherBytes (IV+Content) + HMAC
                    var finalBytes = new byte[cipherBytes.Length + hmacBytes.Length];
                    Buffer.BlockCopy(cipherBytes, 0, finalBytes, 0, cipherBytes.Length);
                    Buffer.BlockCopy(hmacBytes, 0, finalBytes, cipherBytes.Length, hmacBytes.Length);

                    // Base64 + URL Safe replacement
                    var token = Convert.ToBase64String(finalBytes);
                    return token.Replace("+", "-").Replace("/", "_"); 
                }
            }
        }

        public string Decrypt(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;

            try
            {
                // Restore Base64
                token = token.Replace("-", "+").Replace("_", "/");
                switch (token.Length % 4)
                {
                    case 2: token += "=="; break;
                    case 3: token += "="; break;
                }

                var allBytes = Convert.FromBase64String(token);
                
                // Minimum valid length check
                if (allBytes.Length < 48) return null;

                int cipherLength = allBytes.Length - 32;
                var cipherBytes = new byte[cipherLength]; // Contains IV + Content
                var receivedHmac = new byte[32];

                Buffer.BlockCopy(allBytes, 0, cipherBytes, 0, cipherLength);
                Buffer.BlockCopy(allBytes, cipherLength, receivedHmac, 0, 32);

                // Verify HMAC
                using (var hmac = new HMACSHA256(_hmacKey))
                {
                    var computedHmac = hmac.ComputeHash(cipherBytes);
                    if (!RecordatorioEnvio.Infrastructure.Security.SecurityService.ConstantTimeEquals(computedHmac, receivedHmac))
                    {
                        return null; // Tampered
                    }
                }

                // Decrypt
                using (var aes = Aes.Create())
                {
                    aes.KeySize = 256;
                    aes.Key = _key;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    // Extract IV
                    var iv = new byte[16];
                    Buffer.BlockCopy(cipherBytes, 0, iv, 0, 16);
                    aes.IV = iv;

                    // Decrypt content
                    using (var decryptor = aes.CreateDecryptor())
                    using (var ms = new MemoryStream(cipherBytes, 16, cipherBytes.Length - 16))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
