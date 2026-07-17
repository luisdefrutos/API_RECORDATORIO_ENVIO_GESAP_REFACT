using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RecordatorioEnvio.API.Controllers;
using RecordatorioEnvio.Application.DTOs;
using RecordatorioEnvio.Application.Services;
using RecordatorioEnvio.Domain.Entities;
using RecordatorioEnvio.Domain.Interfaces;

namespace RecordatorioEnvio.Tests.Unit.Controllers
{
    [TestClass]
    public class RecordatorioControllerTests
    {
        private Mock<IRecordatorioRepository> _mockRecordatorioRepo;
        private Mock<IEstadoRepository> _mockEstadoRepo;
        private Mock<IEncryptionService> _mockEncryptionService;
        private Mock<IAuditoriaRepository> _mockAuditoriaRepo;
        private Mock<IEmailNotificationService> _mockEmailService;
        private RecordatorioService _realService;
        private RecordatorioController _controller;

        [TestInitialize]
        public void Setup()
        {
            _mockRecordatorioRepo = new Mock<IRecordatorioRepository>();
            _mockEstadoRepo = new Mock<IEstadoRepository>();
            _mockEncryptionService = new Mock<IEncryptionService>();
            _mockAuditoriaRepo = new Mock<IAuditoriaRepository>();
            _mockEmailService = new Mock<IEmailNotificationService>();

            // El servicio de negocio es real, inyectado con repositorios mock
            _realService = new RecordatorioService(
                _mockRecordatorioRepo.Object,
                _mockEstadoRepo.Object,
                _mockEncryptionService.Object,
                _mockAuditoriaRepo.Object
            );

            // El controlador inyecta el servicio real + los mocks directos que necesite
            _controller = new RecordatorioController(
                _realService,
                _mockEncryptionService.Object,
                _mockEmailService.Object
            );
            
            // Simular el request para el Controller (para que Request.Headers funcione en GetCorrId)
            _controller.Request = new System.Net.Http.HttpRequestMessage();
        }

        [TestMethod]
        public void Get_ConTokenValido_DevuelveOkConDatos()
        {
            // Arrange
            string validToken = "TOKEN_VALIDO";
            long validId = 999;
            
            _mockEncryptionService.Setup(s => s.Decrypt(validToken)).Returns(validId.ToString());
            
            _mockRecordatorioRepo.Setup(r => r.GetByIdRecordatorioEnvio(validId))
                .Returns(new RecordatorioEnvioResp { IdRecordatorioEnvio = 50, IdRecordatorioEnvioResp = validId, IdEstadoRecEnvioRespuesta = 1 });

            _mockEstadoRepo.Setup(r => r.GetById(1))
                .Returns(new EstadoRecEnvioRespuesta { Descripcion = "Estado Test" });

            _mockRecordatorioRepo.Setup(r => r.GetDetallesByRecordatorioEnvioId(50))
                .Returns(new List<RecordatorioEnvioDetalle>());

            _mockRecordatorioRepo.Setup(r => r.GetNotasByRespId(validId))
                .Returns(new List<RecordatorioEnvioRespNota>());

            // Act
            var actionResult = _controller.Get(validToken);

            // Assert
            Assert.IsInstanceOfType(actionResult, typeof(OkNegotiatedContentResult<RecordatorioEnvioRespDto>));
            var okResult = actionResult as OkNegotiatedContentResult<RecordatorioEnvioRespDto>;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(validId, okResult.Content.IdRecordatorioEnvioResp);
            Assert.AreEqual("Estado Test", okResult.Content.EstadoDescripcion);
        }

        [TestMethod]
        public void Get_ConTokenInvalido_DevuelveBadRequest()
        {
            // Arrange
            string invalidToken = "TOKEN_CORRUPTO";
            _mockEncryptionService.Setup(s => s.Decrypt(invalidToken)).Returns((string)null);

            // Act
            var actionResult = _controller.Get(invalidToken);

            // Assert
            Assert.IsInstanceOfType(actionResult, typeof(BadRequestErrorMessageResult));
            var badRequestResult = actionResult as BadRequestErrorMessageResult;
            Assert.AreEqual("ID inválido", badRequestResult.Message);
        }

        [TestMethod]
        public async Task Post_ConPayloadValido_GuardaYEnviaEmail()
        {
            // Arrange
            string validTokenPayload = "PAYLOAD_ENCRIPTADO";
            var dtoStr = "{ \"IdRecordatorioEnvio\": 50, \"IdRecordatorioEnvioResp\": 999, \"IdEstadoRecEnvioRespuesta\": 2, \"TitularEmail\": \"test@test.com\" }";
            
            _mockEncryptionService.Setup(s => s.Decrypt(validTokenPayload)).Returns(dtoStr);
            
            _mockEmailService.Setup(e => e.SendEmailAsync("test@test.com", It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            
            _mockRecordatorioRepo.Setup(r => r.GetByIdRecordatorioEnvio(50))
                .Returns(new RecordatorioEnvioResp { IdRecordatorioEnvio = 50, TitularEmail = "test@test.com" });

            _mockRecordatorioRepo.Setup(r => r.GetById(999))
                .Returns(new RecordatorioEnvioResp { IdRecordatorioEnvio = 50, IdRecordatorioEnvioResp = 999, IdEstadoRecEnvioRespuesta = 1 });

            var payload = new RecordatorioEnvio.Application.DTOs.SecurePayload { Data = validTokenPayload };
            string validInmutabilityToken = "TOKEN_INMUTABLE";
            _controller.Request.Headers.Add("X-Inmutability-Token", validInmutabilityToken);
            _mockEncryptionService.Setup(s => s.Decrypt(validInmutabilityToken)).Returns("50");

            // Act
            var actionResult = await _controller.Post(payload);

            // Assert
            Assert.IsTrue(actionResult.GetType().Name.StartsWith("OkNegotiatedContentResult"), "Debería devolver OK");
            _mockRecordatorioRepo.Verify(r => r.SaveAll(It.IsAny<RecordatorioEnvioResp>(), It.IsAny<List<RecordatorioEnvioResItem>>(), It.IsAny<List<RecordatorioEnvioRespNota>>()), Times.Once);
            _mockEmailService.Verify(e => e.SendEmailAsync("test@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task Post_ConReglasDeNegocioInvalidas_DevuelveBadRequest()
        {
            // Arrange
            string validTokenPayload = "PAYLOAD_ENCRIPTADO";
            // DTO inválido porque Email es formato incorrecto
            var dtoStr = "{ \"IdRecordatorioEnvio\": 50, \"IdRecordatorioEnvioResp\": 999, \"IdEstadoRecEnvioRespuesta\": 2, \"TitularEmail\": \"correo-sin-arroba\" }";
            _mockEncryptionService.Setup(s => s.Decrypt(validTokenPayload)).Returns(dtoStr);
            
            var payload = new RecordatorioEnvio.Application.DTOs.SecurePayload { Data = validTokenPayload };
            string validInmutabilityToken = "TOKEN_INMUTABLE";
            _controller.Request.Headers.Add("X-Inmutability-Token", validInmutabilityToken);
            _mockEncryptionService.Setup(s => s.Decrypt(validInmutabilityToken)).Returns("50");

            // Act
            var actionResult = await _controller.Post(payload);

            // Assert
            // Entra en BadRequest custom (Content con status 400 y anonymous type)
            Assert.IsTrue(actionResult.GetType().Name.StartsWith("NegotiatedContentResult"), "Debería devolver BadRequest con contenido de errores");
            _mockRecordatorioRepo.Verify(r => r.SaveAll(It.IsAny<RecordatorioEnvioResp>(), It.IsAny<List<RecordatorioEnvioResItem>>(), It.IsAny<List<RecordatorioEnvioRespNota>>()), Times.Never);
            _mockEmailService.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}
