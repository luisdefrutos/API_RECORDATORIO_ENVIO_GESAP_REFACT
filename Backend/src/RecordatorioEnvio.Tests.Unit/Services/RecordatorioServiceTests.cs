using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using RecordatorioEnvio.Application.Services;
using RecordatorioEnvio.Application.DTOs;
using RecordatorioEnvio.Domain.Entities;
using RecordatorioEnvio.Domain.Interfaces;
using System;

namespace RecordatorioEnvio.Tests.Unit.Services
{
    /// <summary>
    /// Pruebas unitarias para RecordatorioService.
    /// 
    /// CONCEPTO CLAVE: Estas pruebas NO usan la base de datos real.
    /// Usamos "Moq" para simular (mockear) el repositorio Oracle.
    /// Así podemos probar la lógica del servicio de forma aislada y rápida.
    /// 
    /// Estructura de cada test (patrón AAA):
    ///   Arrange (Preparar) → Configure los datos y mocks necesarios
    ///   Act     (Actuar)   → Ejecuta el método a probar
    ///   Assert  (Verificar)→ Comprueba que el resultado es el esperado
    /// </summary>
    [TestClass]
    public class RecordatorioServiceTests
    {
        // ── Campos compartidos por todos los tests ───────────────────────
        private Mock<IRecordatorioRepository> _mockRepo;
        private Mock<IEstadoRepository>       _mockEstadoRepo;
        private Mock<IEncryptionService>      _mockEncryption;  // DIP: Ahora es una abstracción
        private Mock<IAuditoriaRepository>    _mockAuditoria;   // ISP: Separado del repo principal
        private RecordatorioService            _service;

        /// <summary>
        /// [TestInitialize] se ejecuta ANTES de cada test.
        /// Aquí preparamos los mocks y el servicio limpio.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            _mockRepo       = new Mock<IRecordatorioRepository>();
            _mockEstadoRepo = new Mock<IEstadoRepository>();
            _mockEncryption = new Mock<IEncryptionService>(); // Ya no necesita claves reales en Web.config
            _mockAuditoria  = new Mock<IAuditoriaRepository>();

            // Instanciar el servicio con los cuatro mocks inyectados
            _service = new RecordatorioService(
                _mockRepo.Object,
                _mockEstadoRepo.Object,
                _mockEncryption.Object,
                _mockAuditoria.Object);
        }

        // ════════════════════════════════════════════════════════════════
        // HAPPY PATH : GetByIdRecordatorioEnvio
        // Comprueba que el servicio devuelve datos correctamente
        // cuando el repositorio encuentra el registro.
        // ════════════════════════════════════════════════════════════════

        [TestMethod]
        [TestCategory("HappyPath")]
        public void GetByIdRecordatorioEnvio_CuandoExisteRegistro_DevuelveDto()
        {
            // ── Arrange ──────────────────────────────────────────────────
            long idPadre = 4;
            long idHijo = 99;

            // El servicio usa GetByIdRecordatorioEnvio (búsqueda por FK al padre)
            _mockRepo.Setup(r => r.GetByIdRecordatorioEnvio(idPadre))
                     .Returns(new RecordatorioEnvioResp
                     {
                         IdRecordatorioEnvioResp = idHijo,
                         IdRecordatorioEnvio     = idPadre,
                         TitularNif              = "12345678A",
                         TitularRazonSocial      = "Empresa Test SL",
                         IdEstadoRecEnvioRespuesta = 1
                     });

            // Simulamos estado
            _mockEstadoRepo.Setup(e => e.GetById(1))
                           .Returns(new EstadoRecEnvioRespuesta { IdEstadoRecEnvioRespuesta = 1, Descripcion = "Pendiente" });

            // Simulamos notas y detalles
            _mockRepo.Setup(r => r.GetNotasByRespId(idHijo))
                     .Returns(new List<RecordatorioEnvioRespNota> { new RecordatorioEnvioRespNota { DescRecordatorioEnvioNota = "Nota 1" } });
            _mockRepo.Setup(r => r.GetDetallesByRecordatorioEnvioId(It.IsAny<long>()))
                     .Returns(new List<RecordatorioEnvioDetalle>());

            // Simulamos la entidad base RECORDATORIO_ENVIO
            _mockRepo.Setup(r => r.GetRecordatorioEnvioById(idPadre))
                     .Returns(new RecordatorioEnvio.Domain.Entities.RecordatorioEnvio { IdentificadorRecEnvio = "REC-1234", EsEconomico = 1 });

            // ── Act ───────────────────────────────────────────────────────
            RecordatorioEnvioRespDto resultado = _service.GetByIdRecordatorioEnvio(idPadre);

            // ── Assert ────────────────────────────────────────────────────
            Assert.IsNotNull(resultado, "El DTO no debería ser nulo si el registro existe.");
            Assert.AreEqual(idHijo, resultado.IdRecordatorioEnvioResp, "El ID del DTO debe coincidir.");
            Assert.AreEqual("12345678A", resultado.TitularNif, "El NIF del titular debe mapearse correctamente.");
            Assert.AreEqual("Nota 1", resultado.Notas[0].DescRecordatorioEnvioNota, "La nota debería haberse cargado correctamente al buscar por el ID de la respuesta.");
        }

        [TestMethod]
        [TestCategory("HappyPath")]
        public void GetByIdRecordatorioEnvio_CuandoNoExisteRegistro_DevuelveNull()
        {
            // ── Arrange ──────────────────────────────────────────────────
            long idInexistente = 9999;
            // El servicio usa GetByIdRecordatorioEnvio (búsqueda por FK al padre)
            _mockRepo.Setup(r => r.GetByIdRecordatorioEnvio(idInexistente)).Returns((RecordatorioEnvioResp)null);

            // ── Act ───────────────────────────────────────────────────────
            RecordatorioEnvioRespDto resultado = _service.GetByIdRecordatorioEnvio(idInexistente);

            // ── Assert ────────────────────────────────────────────────────
            Assert.IsNull(resultado, "Debe devolver null si el registro no existe en BD.");
        }

        // ════════════════════════════════════════════════════════════════
        // HAPPY PATH : Update
        // Comprueba que SaveAll se llama exactamente una vez con datos válidos
        // ════════════════════════════════════════════════════════════════

        [TestMethod]
        [TestCategory("HappyPath")]
        public void Update_ConDatosValidos_LlamaSaveAllUnaVez()
        {
            // ── Arrange ──────────────────────────────────────────────────
            var dto = new RecordatorioEnvioRespDto
            {
                IdRecordatorioEnvioResp   = 4,
                TitularNif                = "12345678A",
                TitularEmail              = "test@empresa.com",
                FacturarCuentaBanco       = "",
                Notas                     = new List<RecordatorioEnvioRespNotaDto>(),
                Items                     = new List<RecordatorioEnvioResItemDto>()
            };

            // El servicio llama GetById para comprobar que el registro existe y su estado
            _mockRepo.Setup(r => r.GetById(4))
                     .Returns(new RecordatorioEnvioResp
                     {
                         IdRecordatorioEnvioResp       = 4,
                         IdEstadoRecEnvioRespuesta     = 1  // Emitido — estado editable
                     });

            // ── Act ───────────────────────────────────────────────────────
            _service.Update(dto);

            // ── Assert ────────────────────────────────────────────────────
            // Verificamos que el repositorio recibió exactamente 1 llamada a SaveAll
            _mockRepo.Verify(
                r => r.SaveAll(It.IsAny<RecordatorioEnvioResp>(),
                               It.IsAny<IEnumerable<RecordatorioEnvioResItem>>(),
                               It.IsAny<IEnumerable<RecordatorioEnvioRespNota>>()),
                Times.Once,
                "SaveAll debe ser llamado exactamente una vez durante el Update.");
        }

        [TestMethod]
        [TestCategory("HappyPath")]
        public void Update_ConUnaNotaValida_LlamaSaveAllConUnaNote()
        {
            // ── Arrange ──────────────────────────────────────────────────
            var dto = new RecordatorioEnvioRespDto
            {
                IdRecordatorioEnvioResp = 4,
                TitularEmail            = "",
                FacturarCuentaBanco     = "",
                Notas = new List<RecordatorioEnvioRespNotaDto>
                {
                    new RecordatorioEnvioRespNotaDto { DescRecordatorioEnvioNota = "Nota de prueba válida" }
                },
                Items = new List<RecordatorioEnvioResItemDto>()
            };

            // El servicio llama GetById para comprobar que el registro existe y su estado
            _mockRepo.Setup(r => r.GetById(4))
                     .Returns(new RecordatorioEnvioResp
                     {
                         IdRecordatorioEnvioResp   = 4,
                         IdEstadoRecEnvioRespuesta = 1  // Emitido — estado editable
                     });

            IEnumerable<RecordatorioEnvioRespNota> notasCapturadas = null;
            _mockRepo.Setup(r => r.SaveAll(
                It.IsAny<RecordatorioEnvioResp>(),
                It.IsAny<IEnumerable<RecordatorioEnvioResItem>>(),
                It.IsAny<IEnumerable<RecordatorioEnvioRespNota>>()))
            .Callback<RecordatorioEnvioResp, IEnumerable<RecordatorioEnvioResItem>, IEnumerable<RecordatorioEnvioRespNota>>((e, i, n) =>
                {
                    notasCapturadas = n;
                });
            
            // ── Act ───────────────────────────────────────────────────────
            _service.Update(dto);

            // ── Assert ────────────────────────────────────────────────────
            Assert.IsNotNull(notasCapturadas, "Las notas no deben ser nulas.");
            Assert.AreEqual(1, notasCapturadas.Count(), "Debe haber exactamente 1 nota.");
            Assert.AreEqual("Nota de prueba válida",
                            notasCapturadas.First().DescRecordatorioEnvioNota,
                            "El texto de la nota debe conservarse.");
        }

        // ════════════════════════════════════════════════════════════════
        // CORNER CASES : Update con ID inválido
        // ════════════════════════════════════════════════════════════════

        [TestMethod]
        [TestCategory("CornerCase")]
        [ExpectedException(typeof(System.ArgumentException))]
        public void Update_ConIdCero_LanzaArgumentException()
        {
            // ── Arrange ──────────────────────────────────────────────────
            var dto = new RecordatorioEnvioRespDto { IdRecordatorioEnvioResp = 0 };

            // ── Act ───────────────────────────────────────────────────────
            // Se espera excepción → [ExpectedException] la captura automáticamente
            _service.Update(dto);
        }

        [TestMethod]
        [TestCategory("CornerCase")]
        [ExpectedException(typeof(System.ArgumentException))]
        public void Update_ConIdNegativo_LanzaArgumentException()
        {
            var dto = new RecordatorioEnvioRespDto { IdRecordatorioEnvioResp = -5 };
            _service.Update(dto);
        }

        // ════════════════════════════════════════════════════════════════
        // CORNER CASES : Inmutabilidad (Reglas de Estado)
        // ════════════════════════════════════════════════════════════════

        [TestMethod]
        [TestCategory("CornerCase")]
        [ExpectedException(typeof(System.ArgumentException))]
        public void Update_ConEstadoPendienteRevision_LanzaArgumentException_PorqueEsInmutable()
        {
            // ── Arrange ──────────────────────────────────────────────────
            var dto = new RecordatorioEnvioRespDto { IdRecordatorioEnvioResp = 4 };

            // El registro existe pero su estado es 2 (PendienteRevision)
            _mockRepo.Setup(r => r.GetById(4))
                     .Returns(new RecordatorioEnvioResp
                     {
                         IdRecordatorioEnvioResp = 4,
                         IdEstadoRecEnvioRespuesta = 2 // PendienteRevision -> Inmutable
                     });

            // ── Act ───────────────────────────────────────────────────────
            _service.Update(dto);
        }

        [TestMethod]
        [TestCategory("CornerCase")]
        [ExpectedException(typeof(System.ArgumentException))]
        public void Update_ConEstadoTramitado_LanzaArgumentException_PorqueEsInmutable()
        {
            // ── Arrange ──────────────────────────────────────────────────
            var dto = new RecordatorioEnvioRespDto { IdRecordatorioEnvioResp = 4 };

            // El registro existe pero su estado es 3 (Tramitado)
            _mockRepo.Setup(r => r.GetById(4))
                     .Returns(new RecordatorioEnvioResp
                     {
                         IdRecordatorioEnvioResp = 4,
                         IdEstadoRecEnvioRespuesta = 3 // Tramitado -> Inmutable
                     });

            // ── Act ───────────────────────────────────────────────────────
            _service.Update(dto);
        }
        [TestMethod]
        [TestCategory("HappyPath")]
        public void GetRecentLogs_MapeaCorrectamenteDesdeDataTable()
        {
            // ── Arrange ──────────────────────────────────────────────────
            var dt = new DataTable();
            dt.Columns.Add("LOG_ID", typeof(long));
            dt.Columns.Add("LOG_DATE", typeof(DateTime));
            dt.Columns.Add("LOG_LEVEL", typeof(string));
            dt.Columns.Add("MESSAGE", typeof(string));
            dt.Columns.Add("EXCEPTION_MSG", typeof(string));
            dt.Columns.Add("IP_ADDRESS", typeof(string));
            dt.Columns.Add("ENDPOINT", typeof(string));
            dt.Columns.Add("SQL_QUERY", typeof(string));

            dt.Rows.Add(1, DateTime.Now, "INFO", "Log de prueba", null, "127.0.0.1", "/api/test", "SELECT 1");

            _mockAuditoria.Setup(r => r.GetRecentLogs(It.IsAny<int>())).Returns(dt);

            // ── Act ───────────────────────────────────────────────────────
            var logs = _service.GetRecentLogs(1).ToList();

            // ── Assert ────────────────────────────────────────────────────
            Assert.AreEqual(1, logs.Count);
            Assert.AreEqual("INFO", logs[0].LogLevel);
            Assert.AreEqual("Log de prueba", logs[0].Message);
            Assert.AreEqual("127.0.0.1", logs[0].IpAddress);
        }
    }
}
