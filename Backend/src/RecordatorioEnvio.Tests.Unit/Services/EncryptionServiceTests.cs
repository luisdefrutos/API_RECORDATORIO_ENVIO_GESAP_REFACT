using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RecordatorioEnvio.Domain.Interfaces;
using RecordatorioEnvio.Infrastructure.Encryption;
using System;
using System.Configuration;
using System.Reflection;

namespace RecordatorioEnvio.Tests.Unit.Services
{
    [TestClass]
    public class EncryptionServiceTests
    {
        private IEncryptionService _service;

        [TestInitialize]
        public void Setup()
        {
            // Nota: EncryptionService lee desde ConfigurationManager.AppSettings en su constructor.
            // Para testearlo de forma aislada sin depender del App.config de toda la solución,
            // usamos Reflexión para inyectar claves de prueba válidas (32 bytes en Base64).
            
            // Creamos una instancia evitando que el constructor original lance excepción por falta de config
            _service = (IEncryptionService)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(EncryptionService));

            // Claves de 32 bytes en Base64 válidas para la prueba
            byte[] keyBytes = new byte[32];
            byte[] hmacBytes = new byte[32];
            for (int i = 0; i < 32; i++) { keyBytes[i] = (byte)i; hmacBytes[i] = (byte)(31 - i); }

            var fieldKey = typeof(EncryptionService).GetField("_key", BindingFlags.NonPublic | BindingFlags.Instance);
            var fieldHmac = typeof(EncryptionService).GetField("_hmacKey", BindingFlags.NonPublic | BindingFlags.Instance);

            fieldKey.SetValue(_service, keyBytes);
            fieldHmac.SetValue(_service, hmacBytes);
        }

        [TestMethod]
        [TestCategory("HappyPath")]
        public void Encrypt_Decrypt_Roundtrip_DevuelveValorOriginal()
        {
            string originalText = "12345";

            // Act
            string encrypted = _service.Encrypt(originalText);
            string decrypted = _service.Decrypt(encrypted);

            // Assert
            Assert.IsNotNull(encrypted);
            Assert.AreNotEqual(originalText, encrypted, "El texto cifrado debe ser diferente al original");
            Assert.AreEqual(originalText, decrypted, "El valor descifrado no coincide con el original");
        }

        [TestMethod]
        [TestCategory("CornerCase")]
        public void Decrypt_ConTokenInvalido_DevuelveNull()
        {
            string tokenTampered = "token-totalmente-falso-y-sin-sentido-1234567890==";
            
            string decrypted = _service.Decrypt(tokenTampered);

            Assert.IsNull(decrypted, "Un token manipulado debe devolver null, no lanzar excepción");
        }

        [TestMethod]
        [TestCategory("CornerCase")]
        public void Encrypt_ConTextoNuloOVacio_DevuelveNull()
        {
            Assert.IsNull(_service.Encrypt(null));
            Assert.IsNull(_service.Encrypt(""));
        }
    }
}
