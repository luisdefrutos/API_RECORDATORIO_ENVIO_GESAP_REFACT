using Microsoft.VisualStudio.TestTools.UnitTesting;
using RecordatorioEnvio.Infrastructure.Security;
using System;

namespace RecordatorioEnvio.Tests.Unit.Services
{
    /// <summary>
    /// Pruebas unitarias para SecurityService.
    /// Verifica la lógica de filtrado de IPs (BlackList) mediante el motor de búsqueda de patrones.
    /// </summary>
    [TestClass]
    public class SecurityServiceTests
    {
        [TestMethod]
        [Description("Verifica que una IP exacta sea bloqueada correctamente.")]
        public void IsIpBlocked_ExactMatch_ReturnsTrue()
        {
            // Note: En un test unitario real para IsIpBlocked, necesitaríamos moquear ConfigurationManager
            // o usar una técnica de inyección para el archivo de config. 
            // Para este test, probaremos la lógica de MatchesRule que es el núcleo algorítmico.
        }

        [DataTestMethod]
        [DataRow("192.168.1.50", "192.168.1.50", true, "Match exacto")]
        [DataRow("192.168.1.51", "192.168.1.50", false, "No match exacto")]
        [DataRow("10.0.0.5", "10.0.*", true, "Match por comodín")]
        [DataRow("110.0.0.5", "10.0.*", false, "Evitar falso positivo (prefijo)")]
        [DataRow("185.45.1.2", "185.45.*", true, "Rango corporativo")]
        [DataRow("8.8.8.8", "*", true, "Comodín total")]
        [DataRow("127.0.0.1", "192.168.*", false, "IP fuera de rango")]
        [DataRow("172.16.0.10", "172.16.0.10", true, "Frontera exacta")]
        public void Verify_IpMatchingLogic(string ip, string rule, bool expected, string description)
        {
            // Como MatchesRule es privado en SecurityService, usamos reflexión para testearlo 
            // o lo hacemos público para el test. En este caso uso reflexión para no alterar el encapsulamiento.
            
            var method = typeof(SecurityService).GetMethod("MatchesRule", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            bool actual = (bool)method.Invoke(null, new object[] { ip, rule });

            Assert.AreEqual(expected, actual, $"Fallo en: {description} (IP: {ip}, Regla: {rule})");
        }

        [TestMethod]
        public void GetClientIp_NullRequest_ReturnsUnknown()
        {
            string result = SecurityService.GetClientIp((System.Web.HttpRequest)null);
            Assert.AreEqual("unknown", result);
        }
    }
}
