using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using RecordatorioEnvio.Application.Services;
using RecordatorioEnvio.Application.DTOs;
using RecordatorioEnvio.Domain.Interfaces;
using RecordatorioEnvio.Domain.Entities;

namespace RecordatorioEnvio.Tests.Unit.Services
{
    /// <summary>
    /// Pruebas unitarias centradas exclusivamente en las validaciones de negocio.
    /// Aquí verificamos todos los casos de ValidateRules (email, IBAN, notas, etc.)
    /// tanto para el camino feliz (sin errores) como para los casos límite.
    /// </summary>
    [TestClass]
    public class ValidacionesServiceTests
    {
        private Mock<IRecordatorioRepository> _mockRepo;
        private Mock<IEstadoRepository>       _mockEstadoRepo;
        private Mock<IEncryptionService>      _mockEncryption;
        private Mock<IAuditoriaRepository>    _mockAuditoria;
        private RecordatorioService            _service;

        [TestInitialize]
        public void Setup()
        {
            _mockRepo       = new Mock<IRecordatorioRepository>();
            _mockEstadoRepo = new Mock<IEstadoRepository>();
            _mockEncryption = new Mock<IEncryptionService>(); // DIP: mock de la abstracción
            _mockAuditoria  = new Mock<IAuditoriaRepository>(); // ISP: mock del repositorio de auditoría
            _service        = new RecordatorioService(
                _mockRepo.Object,
                _mockEstadoRepo.Object,
                _mockEncryption.Object,
                _mockAuditoria.Object);
        }

        // ════════════════════════════════════════════════════════════════
        // ValidateRules — HAPPY PATH (sin errores esperados)
        // ════════════════════════════════════════════════════════════════

        [TestMethod]
        [TestCategory("HappyPath")]
        public void ValidateRules_DatosCorrecto_DevuelveListaVacia()
        {
            // ── Arrange ──────────────────────────────────────────────────
            var dto = new RecordatorioEnvioRespDto
            {
                TitularEmail          = "contacto@empresa.com",
                RepresentanteEmail    = "gestor@empresa.com",
                FacturarCuentaBanco   = "ES9121000418450200051332"  // IBAN válido
            };

            // ── Act ───────────────────────────────────────────────────────
            var errores = _service.ValidateRules(dto);

            // ── Assert ────────────────────────────────────────────────────
            Assert.AreEqual(0, errores.Count,
                "No deben existir errores si el email e IBAN son correctos.");
        }

        [TestMethod]
        [TestCategory("HappyPath")]
        public void ValidateRules_CamposOpcionales_Vacios_SinError()
        {
            // El email y el IBAN son opcionales — si están vacíos no hay error
            var dto = new RecordatorioEnvioRespDto
            {
                TitularEmail        = "",
                RepresentanteEmail  = "",
                FacturarCuentaBanco = ""
            };

            var errores = _service.ValidateRules(dto);

            Assert.AreEqual(0, errores.Count,
                "Campos opcionales vacíos no deben generar error de validación.");
        }

        // ════════════════════════════════════════════════════════════════
        // ValidateRules — CORNER CASES : Email inválido
        // ════════════════════════════════════════════════════════════════

        [TestMethod]
        [TestCategory("CornerCase")]
        public void ValidateRules_EmailTitularInvalido_DevuelveError()
        {
            var dto = new RecordatorioEnvioRespDto
            {
                TitularEmail        = "email-sin-arroba",  // Email malformado
                RepresentanteEmail  = "",
                FacturarCuentaBanco = ""
            };

            var errores = _service.ValidateRules(dto);

            Assert.IsTrue(errores.Count > 0,
                "Email inválido del Titular debe generar al menos un error.");
        }

        [TestMethod]
        [TestCategory("CornerCase")]
        public void ValidateRules_EmailRepresentanteInvalido_DevuelveError()
        {
            var dto = new RecordatorioEnvioRespDto
            {
                TitularEmail        = "",
                RepresentanteEmail  = "noesunemail",
                FacturarCuentaBanco = ""
            };

            var errores = _service.ValidateRules(dto);

            Assert.IsTrue(errores.Count > 0,
                "Email inválido del Representante debe generar al menos un error.");
        }

        // ════════════════════════════════════════════════════════════════
        // ValidateRules — CORNER CASES : IBAN inválido
        // ════════════════════════════════════════════════════════════════

        [TestMethod]
        [TestCategory("CornerCase")]
        public void ValidateRules_IbanInvalido_DevuelveError()
        {
            // IMPORTANTE: La validación IBAN solo se activa si FacturarFormaPago == "DOMICILIACION"
            var dto = new RecordatorioEnvioRespDto
            {
                TitularEmail        = "",
                RepresentanteEmail  = "",
                FacturarFormaPago   = "DOMICILIACION",   // ← activa la validación de IBAN
                FacturarCuentaBanco = "IBAN-INVALIDO-123"
            };

            var errores = _service.ValidateRules(dto);

            Assert.IsTrue(errores.Count > 0,
                "IBAN con formato incorrecto en modo DOMICILIACION debe generar error de validación.");
        }

        [TestMethod]
        [TestCategory("CornerCase")]
        public void ValidateRules_TodosLosCampos_Invalidos_DevuelveMultiplesErrores()
        {
            var dto = new RecordatorioEnvioRespDto
            {
                TitularEmail        = "notvalid",
                RepresentanteEmail  = "alsonotvalid",
                FacturarCuentaBanco = "NOTANIBAN"
            };

            var errores = _service.ValidateRules(dto);

            Assert.IsTrue(errores.Count >= 2,
                "Con múltiples campos inválidos deben reportarse múltiples errores.");
        }

        // ════════════════════════════════════════════════════════════════
        // ValidateRules — CORNER CASES : Notas
        // (Estos tests verifican la validación de notas en Update)
        // ════════════════════════════════════════════════════════════════

        [TestMethod]
        [TestCategory("CornerCase")]
        [ExpectedException(typeof(ArgumentException))]
        public void Update_NotaConMasDe255Caracteres_LanzaExcepcion()
        {
            // ── Arrange ────────────────────────────────────────────────
            string textoLargo = new string('A', 256); // 256 chars → supera el límite
            var dto = new RecordatorioEnvioRespDto
            {
                IdRecordatorioEnvioResp = 4,
                Notas = new List<RecordatorioEnvioRespNotaDto>
                {
                    new RecordatorioEnvioRespNotaDto { DescRecordatorioEnvioNota = textoLargo }
                },
                Items = new List<RecordatorioEnvioResItemDto>()
            };

            // El registro debe existir en BD y estar en estado Emitido (editable)
            // para que la excepción la lance el validador de notas, no la comprobación de existencia
            _mockRepo.Setup(r => r.GetById(4))
                     .Returns(new RecordatorioEnvioResp
                     {
                         IdRecordatorioEnvioResp   = 4,
                         IdEstadoRecEnvioRespuesta = 1  // Emitido
                     });

            // ── Act (se espera excepción de validación de nota) ────────────────
            _service.Update(dto);
        }

        [TestMethod]
        [TestCategory("CornerCase")]
        [ExpectedException(typeof(ArgumentException))]
        public void Update_NotaConHtml_LanzaExcepcion()
        {
            // ── Arrange ────────────────────────────────────────────────
            var dto = new RecordatorioEnvioRespDto
            {
                IdRecordatorioEnvioResp = 4,
                Notas = new List<RecordatorioEnvioRespNotaDto>
                {
                    new RecordatorioEnvioRespNotaDto
                    {
                        DescRecordatorioEnvioNota = "<script>alert('XSS')</script>"
                    }
                },
                Items = new List<RecordatorioEnvioResItemDto>()
            };

            // El registro debe existir en BD y estar en estado Emitido (editable)
            _mockRepo.Setup(r => r.GetById(4))
                     .Returns(new RecordatorioEnvioResp
                     {
                         IdRecordatorioEnvioResp   = 4,
                         IdEstadoRecEnvioRespuesta = 1  // Emitido
                     });

            // ── Act (se espera excepción de validación XSS) ────────────────
            _service.Update(dto);
        }

        [TestMethod]
        [TestCategory("HappyPath")]
        public void Update_NotaVacia_EsFiltradaYNoLanzaExcepcion()
        {
            // Las notas vacías o de solo espacios deben ignorarse silenciosamente
            var dto = new RecordatorioEnvioRespDto
            {
                IdRecordatorioEnvioResp = 4,
                Notas = new List<RecordatorioEnvioRespNotaDto>
                {
                    new RecordatorioEnvioRespNotaDto { DescRecordatorioEnvioNota = "   " }
                },
                Items = new List<RecordatorioEnvioResItemDto>()
            };

            // El registro debe existir en BD y estar en estado Emitido (editable)
            _mockRepo.Setup(r => r.GetById(4))
                     .Returns(new RecordatorioEnvioResp
                     {
                         IdRecordatorioEnvioResp   = 4,
                         IdEstadoRecEnvioRespuesta = 1  // Emitido
                     });

            // No debe lanzar excepción
            _service.Update(dto);

            // Verificamos que SaveAll se llamó con lista de notas vacía (filtrada)
            _mockRepo.Verify(r => r.SaveAll(
                It.IsAny<RecordatorioEnvio.Domain.Entities.RecordatorioEnvioResp>(),
                It.Is<IEnumerable<RecordatorioEnvio.Domain.Entities.RecordatorioEnvioResItem>>(i => !System.Linq.Enumerable.Any(i)),
                It.Is<IEnumerable<RecordatorioEnvio.Domain.Entities.RecordatorioEnvioRespNota>>(n => !System.Linq.Enumerable.Any(n))),
                Times.Once,
                "Nota con solo espacios debe ser filtrada antes de llamar a SaveAll.");
        }
    }
}
