using System;

namespace RecordatorioEnvio.Application.DTOs
{
    public class RecordatorioEnvioDetalleDto
    {
        public int IdRecordatorioEnvio { get; set; }
        public long IdRecordatorioCicloEnvio { get; set; }
        public string DescripcionTipoActuacion { get; set; }
        public string NombreEquipo { get; set; }
        public DateTime? FechaProximaActuacion { get; set; }
        public int? IdSubtipoActuacion { get; set; }
        public string TipoInspeccion { get; set; }
        public int? AprobadoCliente { get; set; }
        public decimal? PrecioActuacionPrimeraRev { get; set; }
        public decimal? OtrosCostes { get; set; }
        public decimal? PrecioTotal { get; set; }
        public string CentroEmail { get; set; }
    }
}
