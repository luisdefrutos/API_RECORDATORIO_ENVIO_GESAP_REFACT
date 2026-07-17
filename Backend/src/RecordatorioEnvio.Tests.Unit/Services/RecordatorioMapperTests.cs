using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using RecordatorioEnvio.Application.DTOs;
using RecordatorioEnvio.Application.Services;
using RecordatorioEnvio.Domain.Entities;

namespace RecordatorioEnvio.Tests.Unit.Services
{
    /// <summary>
    /// Pruebas unitarias para RecordatorioMapper.
    /// 
    /// OBJETIVO: Garantizar que los datos se transforman correctamente entre
    /// las capas Entity (Oracle) y DTO (JSON). Un error aquí = datos corruptos
    /// en la base de datos sin que salte ninguna excepción.
    /// 
    /// NOTA: El Mapper es internal, accesible gracias a:
    /// [assembly: InternalsVisibleTo("RecordatorioEnvio.Tests.Unit")]
    /// </summary>
    [TestClass]
    public class RecordatorioMapperTests
    {
        // ════════════════════════════════════════════════════════════════
        // ToDto — Mapeo Entity → DTO
        // ════════════════════════════════════════════════════════════════

        [TestMethod]
        [TestCategory("HappyPath")]
        public void ToDto_ConEntityCompleta_MapeaTodosLosCamposPrincipales()
        {
            // ── Arrange ──────────────────────────────────────────────────
            var entity = CrearEntityCompleta();
            string estadoDesc = "Emitido";

            // ── Act ───────────────────────────────────────────────────────
            var dto = RecordatorioMapper.ToDto(entity, estadoDesc);

            // ── Assert ────────────────────────────────────────────────────
            // IDs
            Assert.AreEqual(100, dto.IdRecordatorioEnvioResp, "ID respuesta no coincide.");
            Assert.AreEqual(200, dto.IdRecordatorioEnvio, "ID recordatorio no coincide.");

            // Emplazamiento
            Assert.AreEqual("Fábrica Central", dto.EmplazamientoNombre, "Nombre emplazamiento no coincide.");
            Assert.AreEqual("Calle Mayor 10, Madrid", dto.EmplazamientoDireccion, "Dirección emplazamiento no coincide.");

            // Titular
            Assert.AreEqual("12345678A", dto.TitularNif, "NIF titular no coincide.");
            Assert.AreEqual("Empresa S.A.", dto.TitularRazonSocial, "Razón social titular no coincide.");
            Assert.AreEqual("Calle Ejemplo 1", dto.TitularDireccion, "Dirección titular no coincide.");
            Assert.AreEqual("Juan Pérez", dto.TitularPersonaContacto, "Contacto titular no coincide.");
            Assert.AreEqual("912345678", dto.TitularTelefono, "Teléfono titular no coincide.");
            Assert.AreEqual("juan@empresa.com", dto.TitularEmail, "Email titular no coincide.");

            // Representante — verificar que NO se cruzan con los del Titular
            Assert.AreEqual("Ana López", dto.RepresentantePersonaContacto, "Contacto representante no coincide.");
            Assert.AreEqual("698765432", dto.RepresentanteTelefono, "Teléfono representante no coincide.");
            Assert.AreEqual("ana@gestor.com", dto.RepresentanteEmail, "Email representante no coincide.");
            Assert.AreEqual("87654321B", dto.RepresentanteNif, "NIF representante no coincide.");
            Assert.AreEqual("Gestores S.L.", dto.RepresentanteRazonSocial, "Razón social representante no coincide.");
            Assert.AreEqual("Calle Gestión 5", dto.RepresentanteDireccion, "Dirección representante no coincide.");

            // Estado
            Assert.AreEqual(1, dto.IdEstadoRecEnvioRespuesta, "ID estado no coincide.");
            Assert.AreEqual("Emitido", dto.EstadoDescripcion, "Descripción estado no coincide.");

            // Facturación
            Assert.AreEqual("TITULAR", dto.FacturarA, "FacturarA no coincide.");
            Assert.AreEqual("TRANSFERENCIA", dto.FacturarFormaPago, "Forma de pago no coincide.");
            Assert.AreEqual("ES9121000418450200051332", dto.FacturarCuentaBanco, "IBAN no coincide.");
        }

        [TestMethod]
        [TestCategory("HappyPath")]
        public void ToDto_ConDetallesYNotas_MapeatCorrectamenteLasColecciones()
        {
            // ── Arrange ──────────────────────────────────────────────────
            var entity = CrearEntityCompleta();
            var detalles = new List<RecordatorioEnvioDetalle>
            {
                new RecordatorioEnvioDetalle
                {
                    IdRecordatorioEnvio = 200,
                    NombreEquipo = "Ascensor #1",
                    TipoInspeccion = "PERIÓDICA",
                    PrecioTotal = 350.50m,
                    FechaProximaActuacion = new DateTime(2026, 12, 15)
                },
                new RecordatorioEnvioDetalle
                {
                    IdRecordatorioEnvio = 200,
                    NombreEquipo = "Caldera #3",
                    TipoInspeccion = "INICIAL",
                    PrecioTotal = 500m
                }
            };
            var notas = new List<RecordatorioEnvioRespNota>
            {
                new RecordatorioEnvioRespNota { DescRecordatorioEnvioNota = "Nota importante" }
            };

            // ── Act ───────────────────────────────────────────────────────
            var dto = RecordatorioMapper.ToDto(entity, "Emitido", detalles, notas);

            // ── Assert ────────────────────────────────────────────────────
            Assert.AreEqual(2, dto.Detalles.Count, "Deben mapearse 2 detalles.");
            Assert.AreEqual("Ascensor #1", dto.Detalles[0].NombreEquipo, "Nombre equipo 1 no coincide.");
            Assert.AreEqual(350.50m, dto.Detalles[0].PrecioTotal, "Precio equipo 1 no coincide.");
            Assert.AreEqual("Caldera #3", dto.Detalles[1].NombreEquipo, "Nombre equipo 2 no coincide.");

            Assert.AreEqual(1, dto.Notas.Count, "Debe mapearse 1 nota.");
            Assert.AreEqual("Nota importante", dto.Notas[0].DescRecordatorioEnvioNota, "Texto nota no coincide.");
        }

        [TestMethod]
        [TestCategory("HappyPath")]
        public void ToDto_SinDetallesNiNotas_DevuelveListasVacias()
        {
            // ── Arrange ──────────────────────────────────────────────────
            var entity = CrearEntityCompleta();

            // ── Act (no se pasan detalles ni notas) ───────────────────────
            var dto = RecordatorioMapper.ToDto(entity, "Emitido");

            // ── Assert ────────────────────────────────────────────────────
            Assert.IsNotNull(dto.Detalles, "Detalles no debe ser null.");
            Assert.AreEqual(0, dto.Detalles.Count, "Sin detalles, la lista debe estar vacía.");
            Assert.IsNull(dto.Notas, "Notas debe ser null cuando no se pasan (distinto de lista vacía).");
        }

        // ════════════════════════════════════════════════════════════════
        // ToEntity — Mapeo DTO → Entity
        // ════════════════════════════════════════════════════════════════

        [TestMethod]
        [TestCategory("HappyPath")]
        public void ToEntity_ConDtoCompleto_MapeaCorrectamente()
        {
            // ── Arrange ──────────────────────────────────────────────────
            var dto = new RecordatorioEnvioRespDto
            {
                IdRecordatorioEnvioResp = 999,  // Este será ignorado; se usa el parámetro id
                IdRecordatorioEnvio = 200,
                TitularNif = "12345678A",
                TitularRazonSocial = "Empresa S.A.",
                TitularEmail = "test@empresa.com",
                RepresentanteNif = "87654321B",
                RepresentanteEmail = "rep@gestor.com",
                FacturarA = "TITULAR",
                FacturarFormaPago = "DOMICILIACION",
                FacturarCuentaBanco = "ES9121000418450200051332"
            };
            long idReal = 42;

            // ── Act ───────────────────────────────────────────────────────
            var entity = RecordatorioMapper.ToEntity(dto, idReal);

            // ── Assert ────────────────────────────────────────────────────
            Assert.AreEqual(42, entity.IdRecordatorioEnvioResp,
                "El ID de la entity debe ser el parámetro 'id', no el del DTO.");
            Assert.AreEqual("12345678A", entity.TitularNif, "NIF titular no coincide.");
            Assert.AreEqual("87654321B", entity.RepresentanteNif, "NIF representante no coincide.");
            Assert.AreEqual("ES9121000418450200051332", entity.FacturarCuentaBanco, "IBAN no coincide.");
        }

        // ════════════════════════════════════════════════════════════════
        // MapItems y MapNotas
        // ════════════════════════════════════════════════════════════════

        [TestMethod]
        [TestCategory("HappyPath")]
        public void MapItems_ConListaDeItems_AsignaIdRespCorrectamente()
        {
            // ── Arrange ──────────────────────────────────────────────────
            var itemDtos = new List<RecordatorioEnvioResItemDto>
            {
                new RecordatorioEnvioResItemDto { IdRecordatorioCicloEnvio = 10, AprobadoCliente = -1 },
                new RecordatorioEnvioResItemDto { IdRecordatorioCicloEnvio = 20, AprobadoCliente = 0 }
            };

            // ── Act ───────────────────────────────────────────────────────
            var items = RecordatorioMapper.MapItems(itemDtos, 42);

            // ── Assert ────────────────────────────────────────────────────
            Assert.AreEqual(2, items.Count, "Deben mapearse 2 items.");
            Assert.IsTrue(items.All(i => i.IdRecordatorioEnvioResp == 42),
                "Todos los items deben tener el IdResp = 42.");
            Assert.AreEqual(-1, items[0].AprobadoCliente, "AprobadoCliente del item 1 no coincide.");
        }

        [TestMethod]
        [TestCategory("CornerCase")]
        public void MapItems_ConListaNula_DevuelveListaVacia()
        {
            var items = RecordatorioMapper.MapItems(null, 42);
            Assert.AreEqual(0, items.Count, "Con input null, debe devolver lista vacía.");
        }

        [TestMethod]
        [TestCategory("HappyPath")]
        public void MapNotas_ConNotasValidas_FiltraVaciasYAsignaId()
        {
            // ── Arrange ──────────────────────────────────────────────────
            var notaDtos = new List<RecordatorioEnvioRespNotaDto>
            {
                new RecordatorioEnvioRespNotaDto { DescRecordatorioEnvioNota = "Nota válida" },
                new RecordatorioEnvioRespNotaDto { DescRecordatorioEnvioNota = "   " },  // Vacía → filtrada
                new RecordatorioEnvioRespNotaDto { DescRecordatorioEnvioNota = "Otra nota" }
            };

            // ── Act ───────────────────────────────────────────────────────
            var notas = RecordatorioMapper.MapNotas(notaDtos, 42);

            // ── Assert ────────────────────────────────────────────────────
            Assert.AreEqual(2, notas.Count, "Solo las notas con texto deben mapearse (la vacía se filtra).");
            Assert.IsTrue(notas.All(n => n.IdRecordatorioEnvioResp == 42),
                "Todas las notas deben tener el IdResp = 42.");
            Assert.AreEqual("Nota válida", notas[0].DescRecordatorioEnvioNota,
                "El texto debe ser trimmed.");
        }

        [TestMethod]
        [TestCategory("CornerCase")]
        public void MapNotas_ConListaNula_DevuelveListaVacia()
        {
            var notas = RecordatorioMapper.MapNotas(null, 42);
            Assert.AreEqual(0, notas.Count, "Con input null, debe devolver lista vacía.");
        }

        // ════════════════════════════════════════════════════════════════
        // ToLogDtoList — Mapeo DataTable → List<LogEntryDto>
        // ════════════════════════════════════════════════════════════════

        [TestMethod]
        [TestCategory("CornerCase")]
        public void ToLogDtoList_ConDataTableNulo_DevuelveListaVacia()
        {
            var logs = RecordatorioMapper.ToLogDtoList(null);
            Assert.AreEqual(0, logs.Count, "Con DataTable null, debe devolver lista vacía.");
        }

        // ════════════════════════════════════════════════════════════════
        // Helpers
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Crea una entity con TODOS los campos rellenos para verificar mapeos completos.
        /// Los valores de Titular y Representante son intencionalmente distintos
        /// para detectar cruces de datos entre secciones.
        /// </summary>
        private static RecordatorioEnvioResp CrearEntityCompleta()
        {
            return new RecordatorioEnvioResp
            {
                IdRecordatorioEnvioResp = 100,
                IdRecordatorioEnvio = 200,
                FechaEmision = new DateTime(2026, 1, 15),
                FechaRespuestaCliente = new DateTime(2026, 7, 10),

                EmplazamientoNombre = "Fábrica Central",
                EmplazamientoDireccion = "Calle Mayor 10, Madrid",

                // Titular — valores únicos
                TitularNif = "12345678A",
                TitularRazonSocial = "Empresa S.A.",
                TitularDireccion = "Calle Ejemplo 1",
                TitularPersonaContacto = "Juan Pérez",
                TitularTelefono = "912345678",
                TitularEmail = "juan@empresa.com",

                // Representante — valores DISTINTOS al titular
                RepresentantePersonaContacto = "Ana López",
                RepresentanteTelefono = "698765432",
                RepresentanteEmail = "ana@gestor.com",
                RepresentanteNif = "87654321B",
                RepresentanteRazonSocial = "Gestores S.L.",
                RepresentanteDireccion = "Calle Gestión 5",

                IdEstadoRecEnvioRespuesta = 1,
                ModificadoEmplazamiento = 0,
                ModificadoTitular = 0,
                ModificadoRepresentante = 0,

                FacturarA = "TITULAR",
                FacturarAOtroDescripcion = "",
                FacturarFormaPago = "TRANSFERENCIA",
                FacturarCuentaBanco = "ES9121000418450200051332"
            };
        }
    }
}
