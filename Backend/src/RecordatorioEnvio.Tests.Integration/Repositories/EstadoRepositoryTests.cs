using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using RecordatorioEnvio.Infrastructure.Repositories;
using RecordatorioEnvio.Infrastructure.Data;

namespace RecordatorioEnvio.Tests.Integration.Repositories
{
    /// <summary>
    /// Tests de integración para EstadoRepository.
    /// Verifican que la tabla de estados se puede leer correctamente desde Oracle.
    /// </summary>
    [TestClass]
    public class EstadoRepositoryTests
    {
        // MSTest inyecta TestContext automáticamente — su output aparece siempre en el panel de detalles
        public TestContext TestContext { get; set; }

        private EstadoRepository _repository;

        [TestInitialize]
        public void Setup()
        {
            var factory = new OracleConnectionFactory();
            _repository = new EstadoRepository(factory);
        }

        // ════════════════════════════════════════════════════════════════
        // HAPPY PATH : Lectura de catálogo de estados
        // ════════════════════════════════════════════════════════════════

        [TestMethod]
        [TestCategory("Integration")]
        public void GetAll_DevuelveTodosLosEstados()
        {
            // ── Act ───────────────────────────────────────────────────────
            var estados = _repository.GetAll().ToList();

            // ── Assert ────────────────────────────────────────────────────
            Assert.IsNotNull(estados, "La lista de estados no debe ser null.");
            Assert.IsTrue(estados.Count > 0,
                "Debe haber al menos 1 estado en la tabla de catálogo.");

            TestContext.WriteLine($"[OK] Estados encontrados en catálogo: {estados.Count}");
            foreach (var e in estados)
            {
                TestContext.WriteLine($"     ID={e.IdEstadoRecEnvioRespuesta,-3} → {e.Descripcion}");
            }
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void GetAll_TodosLosEstados_TienenDescripcionNoVacia()
        {
            var estados = _repository.GetAll().ToList();
            Assert.IsNotNull(estados);

            foreach (var estado in estados)
            {
                Assert.IsFalse(
                    string.IsNullOrWhiteSpace(estado.Descripcion),
                    $"El estado ID={estado.IdEstadoRecEnvioRespuesta} no debería tener descripción vacía.");
            }
        }

        // ════════════════════════════════════════════════════════════════
        // HAPPY PATH + CORNER CASE : GetById
        // ════════════════════════════════════════════════════════════════

        [TestMethod]
        [TestCategory("Integration")]
        public void GetById_ConIdUno_DevuelveEstado()
        {
            // Asumimos que el estado con ID=1 existe (primer estado del catálogo)
            var estado = _repository.GetById(1);

            Assert.IsNotNull(estado, "El estado con ID=1 debe existir en el catálogo.");
            Assert.IsTrue(estado.IdEstadoRecEnvioRespuesta == 1);
            TestContext.WriteLine($"[OK] Estado ID=1 → '{estado.Descripcion}'");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void GetById_ConIdInexistente_DevuelveNull()
        {
            var estado = _repository.GetById(999999);

            Assert.IsNull(estado,
                "Un ID de estado que no existe debe devolver null.");
        }
    }
}
