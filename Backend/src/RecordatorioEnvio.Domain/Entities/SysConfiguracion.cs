namespace RecordatorioEnvio.Domain.Entities
{
    public class SysConfiguracion
    {
        public long IdSysConfiguracion { get; set; }
        public int EsBbddReal { get; set; }
        public string EmailNotificacionesDireccion { get; set; }
        public string EmailNotificacionesUsuario { get; set; }
        public string EmailNotificacionesPassword { get; set; }
        public string ServidorCorreoDireccion { get; set; }
        public string ServidorCorreoPuerto { get; set; }
        public int EmailSsl { get; set; }

        public string EmailLog { get; set; }

        public string EmailEnvioGir { get; set; }

        public bool IsProduccion => EsBbddReal == 1;
        public bool SslHabilitado => EmailSsl == 1;
    }
}
