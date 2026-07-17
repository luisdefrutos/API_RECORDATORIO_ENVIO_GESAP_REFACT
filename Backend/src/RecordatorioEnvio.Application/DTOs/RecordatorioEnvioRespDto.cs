using System;
using System.Collections.Generic;

namespace RecordatorioEnvio.Application.DTOs
{
    public class RecordatorioEnvioRespDto
    {
        public long IdRecordatorioEnvioResp { get; set; }
        
        public long IdRecordatorioEnvio { get; set; } // FK
        
        public DateTime? FechaEmision { get; set; }
        public DateTime? FechaRespuestaCliente { get; set; }
        public DateTime? FechaTramitacion { get; set; }
        
        public string EmplazamientoNombre { get; set; }
        public string EmplazamientoDireccion { get; set; }
        
        public string TitularNif { get; set; }
        public string TitularRazonSocial { get; set; }
        public string TitularDireccion { get; set; }
        public string TitularPersonaContacto { get; set; }
        public string TitularTelefono { get; set; }
        public string TitularEmail { get; set; }
        
        // REFACTOR: Gestor -> Representante
        public string RepresentantePersonaContacto { get; set; }
        public string RepresentanteTelefono { get; set; }
        public string RepresentanteEmail { get; set; }
        public string RepresentanteNif { get; set; }
        public string RepresentanteRazonSocial { get; set; }
        public string RepresentanteDireccion { get; set; }
        
        public long IdEstadoRecEnvioRespuesta { get; set; }
        public string EstadoDescripcion { get; set; }
        
        public int? ModificadoEmplazamiento { get; set; }
        public int? ModificadoTitular { get; set; }
        public int? ModificadoRepresentante { get; set; } // Antes ModificadoGestor
        
        public string FacturarA { get; set; }
        public string FacturarAOtroDescripcion { get; set; }
        public string FacturarFormaPago { get; set; }
        public string FacturarCuentaBanco { get; set; }

        // Campo obtenido de RECORDATORIO_ENVIO (tabla padre)
        public string IdentificadorRecEnvio { get; set; }

        /// <summary>Entidad base (padre) completa (RECORDATORIO_ENVIO)</summary>
        public RecordatorioEnvioDto RecordatorioEnvio { get; set; }

        public List<RecordatorioEnvioDetalleDto> Detalles { get; set; }

        /// <summary>Notas de texto libre (RECORDATORIO_ENVIO_RESP_NOTA)</summary>
        public List<RecordatorioEnvioRespNotaDto> Notas { get; set; }

        /// <summary>
        /// Estado de los checkboxes del grid (RECORDATORIO_ENVIO_RESP_ITEM).
        /// Solo se usa en POST (guardado). En GET, APROBADO_CLIENTE viene embebido en cada Detalle.
        /// </summary>
        public List<RecordatorioEnvioResItemDto> Items { get; set; }
    }
}
