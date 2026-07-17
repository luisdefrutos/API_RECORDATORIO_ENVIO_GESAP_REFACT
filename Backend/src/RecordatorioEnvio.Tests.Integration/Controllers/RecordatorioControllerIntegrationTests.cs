using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using System.Web.Http.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RecordatorioEnvio.API.Controllers;
using RecordatorioEnvio.Application.DTOs;
using RecordatorioEnvio.Application.Services;
using RecordatorioEnvio.Domain.Entities;
using RecordatorioEnvio.Domain.Interfaces;
using RecordatorioEnvio.Infrastructure.Data;
using RecordatorioEnvio.Infrastructure.Logging;
using RecordatorioEnvio.Infrastructure.Repositories;
using RecordatorioEnvio.Infrastructure.Encryption;

namespace RecordatorioEnvio.Tests.Integration.Controllers
{
    [TestClass]
    public class RecordatorioControllerIntegrationTests
    {
        private IRecordatorioRepository _realRecordatorioRepo;
        private IEstadoRepository _realEstadoRepo;
        private IEncryptionService _realEncryptionService;
        private IAuditoriaRepository _realAuditoriaRepo;
        private Mock<IEmailNotificationService> _mockEmailService;
        
        private RecordatorioService _realService;
        private RecordatorioController _controller;
        private List<long> _testIds;

        [TestInitialize]
        public void Setup()
        {
            // 1. Inicializar Repositorios Reales (Oracle)
            var factory = new OracleConnectionFactory();
            var repo = new RecordatorioRepository(factory);
            _realRecordatorioRepo = repo;
            _realAuditoriaRepo = repo; // RecordatorioRepository implementa IAuditoriaRepository
            _realEstadoRepo = new EstadoRepository(factory);
            
            // 2. Inicializar Servicio de Encriptación Real (Usa App.config)
            _realEncryptionService = new EncryptionService();

            // 3. Mockear SOLO el servicio de Email para no hacer spam real a servidores SMTP corporativos durante la Integración
            _mockEmailService = new Mock<IEmailNotificationService>();
            _mockEmailService.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                             .ReturnsAsync(true);

            // 4. Inicializar Servicio de Negocio Real
            _realService = new RecordatorioService(
                _realRecordatorioRepo,
                _realEstadoRepo,
                _realEncryptionService,
                _realAuditoriaRepo
            );

            // 5. Inicializar Controlador
            _controller = new RecordatorioController(
                _realService,
                _realEncryptionService,
                _mockEmailService.Object
            );

            // 6. Configurar Request mockeado para el logger
            _controller.Request = new System.Net.Http.HttpRequestMessage();
            _controller.Request.Headers.Add("X-Correlation-ID", "TEST-INTEGRACION-CTRL");

            // 7. Cargar IDs reales dinámicos de Oracle para las pruebas (solo pendientes, estado = 1)
            _testIds = _realRecordatorioRepo.GetAll()
                                            .Where(x => x.IdEstadoRecEnvioRespuesta == 1)
                                            .Take(5)
                                            .Select(x => x.IdRecordatorioEnvio)
                                            .ToList();
        }

        [TestMethod]
        public void Get_ConTokenRealValido_DevuelveOkConDatosOracle()
        {
            if (_testIds == null || !_testIds.Any())
            {
                Assert.Inconclusive("No hay datos en Oracle con Estado 1 para ejecutar esta prueba de integración.");
                return;
            }

            // Arrange: Cogemos un ID real y lo encriptamos de verdad
            long realId = _testIds.First();
            string validRealToken = _realEncryptionService.Encrypt(realId.ToString());

            // Act
            var actionResult = _controller.Get(validRealToken);

            // Assert
            Assert.IsInstanceOfType(actionResult, typeof(OkNegotiatedContentResult<RecordatorioEnvioRespDto>));
            var okResult = actionResult as OkNegotiatedContentResult<RecordatorioEnvioRespDto>;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(realId, okResult.Content.IdRecordatorioEnvio); // Validar por el ID padre
            Assert.IsFalse(string.IsNullOrEmpty(okResult.Content.EstadoDescripcion), "La descripción del estado de Oracle no debería estar vacía");
        }

        [TestMethod]
        public void Get_ConTokenRealCorrupto_DevuelveBadRequest()
        {
            // Arrange
            string invalidToken = "U2FsdGVkX19XXXYYYZZZTokenMalo=";

            // Act
            var actionResult = _controller.Get(invalidToken);

            // Assert
            Assert.IsInstanceOfType(actionResult, typeof(BadRequestErrorMessageResult));
        }

        [TestMethod]
        public async Task Post_GuardaDatosEnOracleConExito_RollbackAutomatico()
        {
            if (_testIds == null || !_testIds.Any())
            {
                Assert.Inconclusive("No hay datos en Oracle con Estado 1 para ejecutar esta prueba.");
                return;
            }

            long realId = _testIds.First();

            // Envolvemos todo en TransactionScope para que NADA se guarde realmente en Oracle
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                // Arrange
                var originalEntity = _realRecordatorioRepo.GetByIdRecordatorioEnvio(realId);
                
                // Construimos payload JSON
                string jsonPayload = "{ \"IdRecordatorioEnvio\": " + originalEntity.IdRecordatorioEnvio + ", \"IdRecordatorioEnvioResp\": " + originalEntity.IdRecordatorioEnvioResp + ", \"IdEstadoRecEnvioRespuesta\": 2, \"TitularEmail\": \"integracion@test.com\" }";
                string encryptedPayload = _realEncryptionService.Encrypt(jsonPayload);
                var securePayload = new RecordatorioEnvio.Application.DTOs.SecurePayload { Data = encryptedPayload };

                // Simular el token inmutable en la cabecera (cifrar el ID real)
                string encryptedInmutabilityToken = _realEncryptionService.Encrypt(originalEntity.IdRecordatorioEnvio.ToString());
                _controller.Request.Headers.Add("X-Inmutability-Token", encryptedInmutabilityToken);

                var actionResult = await _controller.Post(securePayload);

                // Assert
                Assert.IsTrue(actionResult.GetType().Name.StartsWith("OkNegotiatedContentResult"), "Debería devolver OK");

                // Verificar que el EmailService falso fue llamado para asegurar el flujo completo
                _mockEmailService.Verify(e => e.SendEmailAsync(It.Is<string>(s => s.Contains("integracion@test.com")), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

                // No llamamos a scope.Complete(), así que todo el UPDATE se deshace automáticamente (Rollback).
            }
        }
    }
}
