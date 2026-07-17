using System;

namespace RecordatorioEnvio.Domain.Entities
{
    public class RecordatorioEnvioResp
    {
        public long IdRecordatorioEnvioResp { get; set; }
        public long IdRecordatorioEnvio { get; set; }
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
        

        // REFACTOR: Gestor -> Representante (Mapping to GESTOR columns in DB)
        public string RepresentantePersonaContacto { get; set; }
        public string RepresentanteTelefono { get; set; }
        public string RepresentanteEmail { get; set; }
        public string RepresentanteNif { get; set; }
        public string RepresentanteRazonSocial { get; set; }
        public string RepresentanteDireccion { get; set; }
        
        public long IdEstadoRecEnvioRespuesta { get; set; }
        
        public int? ModificadoEmplazamiento { get; set; }
        public int? ModificadoTitular { get; set; }
        public int? ModificadoRepresentante { get; set; } // Antes ModificadoGestor
        
        public string FacturarA { get; set; }
        public string FacturarAOtroDescripcion { get; set; }
        public string FacturarFormaPago { get; set; }
        public string FacturarCuentaBanco { get; set; }

        // Propiedad de navegación (opcional, pero útil)
        public virtual EstadoRecEnvioRespuesta Estado { get; set; }
    }
}
