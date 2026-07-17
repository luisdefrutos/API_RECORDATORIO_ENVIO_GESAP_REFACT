using System;

namespace RecordatorioEnvio.Domain.Entities
{
    public class RecordatorioEnvio
    {
        public long IdRecordatorioEnvio { get; set; }
        public long? IdEmplazamiento { get; set; }
        public long? IdTipoActuacion { get; set; }
        public DateTime? FechaEnvioRecordatorio { get; set; }
        public string TipoEnvio { get; set; }
        public string Destinatario { get; set; }
        public string CondicionesEcoCorreccion { get; set; }
        public string EmailDestinatario { get; set; }
        public string IdentificadorRecEnvio { get; set; }
        public long? IdCentro { get; set; }
        public int? EsEconomico { get; set; }
        public string CondicionesEcoGenerales { get; set; }
    }
}
