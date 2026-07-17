using Microsoft.VisualStudio.TestTools.UnitTesting;
using RecordatorioEnvio.Application.DTOs;
using RecordatorioEnvio.Application.Services;
using System.Collections.Generic;

namespace RecordatorioEnvio.Tests.Unit.Services
{
    [TestClass]
    public class EmailTemplateBuilderTests
    {
        [TestMethod]
        [TestCategory("HappyPath")]
        public void ConstruirCuerpoEmail_ConDtoCompleto_ContieneTodosLosDatosClave()
        {
            var dto = new RecordatorioEnvioRespDto
            {
                IdentificadorRecEnvio = "REC-2026-001",
                TitularNif = "12345678Z",
                TitularRazonSocial = "Empresa de Pruebas",
                EmplazamientoNombre = "Planta de Tratamiento",
                Detalles = new List<RecordatorioEnvioDetalleDto>
                {
                    new RecordatorioEnvioDetalleDto
                    {
                        NombreEquipo = "Caldera Industrial",
                        TipoInspeccion = "INICIAL",
                        PrecioTotal = 1500m
                    }
                },
                Notas = new List<RecordatorioEnvioRespNotaDto>
                {
                    new RecordatorioEnvioRespNotaDto { DescRecordatorioEnvioNota = "Nota muy importante para el inspector" }
                }
            };

            var html = EmailTemplateBuilder.ConstruirCuerpoEmail(dto);

            Assert.IsNotNull(html);
            Assert.IsTrue(html.Contains("REC-2026-001"), "Falta el identificador");
            Assert.IsTrue(html.Contains("12345678Z"), "Falta el NIF del titular");
            Assert.IsTrue(html.Contains("Empresa de Pruebas"), "Falta la razón social");
            Assert.IsTrue(html.Contains("Planta de Tratamiento"), "Falta el emplazamiento");
            Assert.IsTrue(html.Contains("Caldera Industrial"), "Falta el equipo");
            Assert.IsTrue(html.Contains("Nota muy importante"), "Falta la nota");
        }

        [TestMethod]
        [TestCategory("CornerCase")]
        public void ConstruirCuerpoEmail_SinDetalles_NoLanzaExcepcion()
        {
            var dto = new RecordatorioEnvioRespDto
            {
                IdentificadorRecEnvio = "REC-2026-002",
                Detalles = null, // Esto podría pasar si falla la carga o no hay equipos
                Notas = null
            };

            var html = EmailTemplateBuilder.ConstruirCuerpoEmail(dto);

            Assert.IsNotNull(html);
            Assert.IsTrue(html.Contains("No hay equipos detallados"), "Debe mostrar el mensaje por defecto cuando no hay equipos");
        }
    }
}
