using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Configuration;
using System.Linq;
using RecordatorioEnvio.Infrastructure.Repositories;
using RecordatorioEnvio.Infrastructure.Data;
using RecordatorioEnvio.Domain.Entities;
using System;
using System.Transactions;
using System.Collections.Generic;

namespace RecordatorioEnvio.Tests.Integration.Repositories
{
    /// <summary>
    /// Tests de INTEGRACIÓN para RecordatorioRepository.
    ///
    /// DIFERENCIA CON LOS UNITARIOS:
    ///   Estos tests hablan con la BD Oracle REAL.
    ///   Si Oracle no está disponible o la conexión falla, los tests fallarán.
    ///   Son más lentos (ms vs segundos) pero prueban el SQL real.
    ///
    /// REQUISITOS PARA EJECUTAR:
    ///   1. Conexión activa al Oracle de desarrollo
    ///   2. App.config con la cadena de conexión correcta
    ///   3. Ejecutar solo en local — nunca en un servidor compartido
    ///
    /// PATRÓN: [TestCategory("Integration")] permite filtrar y
    ///          excluir estos tests cuando no hay conexión disponible.
    /// </summary>
    [TestClass]
    public class RecordatorioRepositoryTests
    {
        // MSTest inyecta TestContext automáticamente — su output aparece siempre en el panel de detalles
        public TestContext TestContext { get; set; }

        private RecordatorioRepository _repository;
        private List<long> _testIds;

        [TestInitialize]
        public void Setup()
        {
            var factory = new OracleConnectionFactory();
            _repository = new RecordatorioRepository(factory);

            // Cargamos dinámicamente hasta 10 IDs reales de la base de datos
            _testIds = _repository.GetAll()
                                  .Take(10)
                                  .Select(x => x.IdRecordatorioEnvioResp)
                                  .ToList();
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            // WORKAROUND: Forzamos a Oracle a destruir su pool interno de hilos en segundo plano
            // antes de que MSTest destruya el AppDomain. Esto evita el molesto ruido visual:
            // "System.AppDomainUnloadedException" en la consola del .bat.
            Oracle.ManagedDataAccess.Client.OracleConnection.ClearAllPools();
            System.Threading.Thread.Sleep(200); // Pequeño respiro para que el hilo muera
        }

        // ════════════════════════════════════════════════════════════════
        // HAPPY PATH : Lectura de datos
        // ════════════════════════════════════════════════════════════════

        [TestMethod]
        [TestCategory("Integration")]
        public void GetById_ConIdsExistentes_DevuelveRegistrosReales()
        {
            if (!_testIds.Any()) Assert.Inconclusive("No hay datos en la BD para probar.");

            foreach (var id in _testIds)
            {
                // ── Act ───────────────────────────────────────────────────────
                var resultado = _repository.GetById(id);

                // ── Assert ────────────────────────────────────────────────────
                Assert.IsNotNull(resultado, $"Debe existir un registro con ID={id} en BD.");
                Assert.AreEqual(id, resultado.IdRecordatorioEnvioResp, "El ID devuelto debe coincidir con el solicitado.");
                Assert.IsTrue(resultado.IdEstadoRecEnvioRespuesta > 0, "Debe tener un estado válido asignado.");

                TestContext.WriteLine($"[OK] Registro ID={id} encontrado. Estado: {resultado.IdEstadoRecEnvioRespuesta}");
            }
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void GetById_ConIdInexistente_DevuelveNull()
        {
            var resultado = _repository.GetById(999999999);
            Assert.IsNull(resultado, "Un ID que no existe en BD debe devolver null.");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void GetDetallesByRecordatorioEnvioId_ConIdsExistentes_DevuelveListas()
        {
            if (!_testIds.Any()) Assert.Inconclusive("No hay datos en la BD para probar.");

            foreach (var id in _testIds)
            {
                var resp = _repository.GetById(id);
                Assert.IsNotNull(resp, $"Precondición: el registro padre {id} debe existir.");

                var detalles = _repository.GetDetallesByRecordatorioEnvioId(resp.IdRecordatorioEnvio).ToList();

                Assert.IsNotNull(detalles, "La lista de detalles no debe ser null.");
                TestContext.WriteLine($"[OK] Detalles para RecordatorioEnvioId={resp.IdRecordatorioEnvio} (Padre: {id}): {detalles.Count} registros");
            }
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void GetNotasByRespId_ConIdsExistentes_DevuelveListas()
        {
            if (!_testIds.Any()) Assert.Inconclusive("No hay datos en la BD para probar.");

            foreach (var id in _testIds)
            {
                var notas = _repository.GetNotasByRespId(id).ToList();

                Assert.IsNotNull(notas, "La lista de notas no debe ser null.");
                TestContext.WriteLine($"[OK] Notas encontradas para RespId={id}: {notas.Count} nota(s)");
            }
        }

        // ════════════════════════════════════════════════════════════════
        // HAPPY PATH : Verificar conexión a Oracle
        // ════════════════════════════════════════════════════════════════

        [TestMethod]
        [TestCategory("Integration")]
        public void Oracle_ConexionDirecta_EsExitosa()
        {
            // Test de "nivel 0" — verificar que hay conectividad antes de ejecutar el resto
            var factory = new OracleConnectionFactory();
            try
            {
                using (var conn = factory.CreateConnection())
                {
                    conn.Open();
                    Assert.AreEqual(System.Data.ConnectionState.Open, conn.State,
                        "La conexión a Oracle debe poderse abrir.");
                    TestContext.WriteLine("[OK] Conexión a Oracle establecida correctamente.");
                }
            }
            catch (Exception ex)
            {
                Assert.Fail($"No se pudo conectar a Oracle: {ex.Message}");
            }
        }

        // ════════════════════════════════════════════════════════════════
        // CORNER CASE : GetAll — verificar que devuelve registros
        // ════════════════════════════════════════════════════════════════

        [TestMethod]
        [TestCategory("Integration")]
        public void GetAll_DevuelveMasDeUnRegistro()
        {
            var todos = _repository.GetAll().ToList();

            Assert.IsNotNull(todos, "La lista no debe ser null.");
            Assert.IsTrue(todos.Count > 0,
                "Debe haber al menos 1 recordatorio en la BD.");
            TestContext.WriteLine($"[OK] Total de recordatorios en BD: {todos.Count} registros");
        }

        // ════════════════════════════════════════════════════════════════
        // TEST DE ESCRITURA (CON TRANSACTION SCOPE)
        // ════════════════════════════════════════════════════════════════

        [TestMethod]
        [TestCategory("Integration")]
        public void SaveAll_GuardaFormularioItemsYNotas_SinDejarBasuraEnBD()
        {
            if (!_testIds.Any()) Assert.Inconclusive("No hay datos en la BD para probar.");
            long testId = _testIds.First();

            // Este test hace operaciones Reales de Base de datos.
            // Para evitar llenar la BD de test con basura, usamos TransactionScope.
            // Cuando termine el bloque USING, se hará Rollback automáticamente.
            using (var scope = new TransactionScope())
            {
                // ── Arrange ──────────────────────────────────────────────────
                var respOriginal = _repository.GetById(testId);
                Assert.IsNotNull(respOriginal, "Precondición fallida: El registro original debe existir.");

                // Modificamos algo temporalmente
                respOriginal.TitularPersonaContacto = "TEST_INTEGRACION_" + DateTime.Now.Ticks;

                // Para evitar errores de Foreign Key en la BD (ORA-02291), obtenemos un IdRecordatorioCicloEnvio válido
                // que corresponda a este RecordatorioEnvio.
                var detallesExistentes = _repository.GetDetallesByRecordatorioEnvioId(respOriginal.IdRecordatorioEnvio).ToList();
                long cicloIdValido = detallesExistentes.Any() ? detallesExistentes.First().IdRecordatorioCicloEnvio : 0;

                // Creamos un Item apuntando por FK al ID del Respuesta (IdRecordatorioEnvioResp)
                var items = new List<RecordatorioEnvioResItem>
                {
                    new RecordatorioEnvioResItem
                    {
                        IdRecordatorioEnvioResp = respOriginal.IdRecordatorioEnvioResp, // La FK!
                        IdRecordatorioCicloEnvio = cicloIdValido, // Un Ciclo válido para evitar error FK
                        AprobadoCliente = 1
                    }
                };

                // Creamos una nota apuntando por FK
                var notas = new List<RecordatorioEnvioRespNota>
                {
                    new RecordatorioEnvioRespNota
                    {
                        IdRecordatorioEnvioResp = respOriginal.IdRecordatorioEnvioResp, // La FK!
                        DescRecordatorioEnvioNota = "Nota de Test de Integracion"
                    }
                };

                // ── Act ───────────────────────────────────────────────────────
                _repository.SaveAll(respOriginal, items, notas);

                // ── Assert ────────────────────────────────────────────────────
                // Buscamos de nuevo en la BD dentro de esta misma transacción a ver si está
                var respActualizado = _repository.GetById(testId);
                Assert.AreEqual(respOriginal.TitularPersonaContacto, respActualizado.TitularPersonaContacto, "El update del titular falló.");

                var notasActualizadas = _repository.GetNotasByRespId(testId).ToList();
                Assert.IsTrue(notasActualizadas.Any(n => n.DescRecordatorioEnvioNota == "Nota de Test de Integracion"), "El guardado de las notas usando la FK IdRecordatorioEnvioResp falló.");

                TestContext.WriteLine("[OK] La operación SaveAll completó con éxito (Update Resp + Merge Item + Delete/Insert Notas).");
                TestContext.WriteLine("[OK] Saliendo del TransactionScope. Se hará ROLLBACK automático.");
            }
            
            // Verificamos que el Rollback haya funcionado
            var respDespuesDelRollback = _repository.GetById(testId);
            Assert.AreNotEqual("TEST_INTEGRACION_", respDespuesDelRollback.TitularPersonaContacto?.Substring(0, 17) ?? "", "¡El ROLLBACK falló! Hay datos corruptos en la BD.");
        }
    }
}
