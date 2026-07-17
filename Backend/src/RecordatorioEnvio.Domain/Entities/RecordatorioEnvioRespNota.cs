namespace RecordatorioEnvio.Domain.Entities
{
    /// <summary>
    /// Tabla: RECORDATORIO_ENVIO_RESP_NOTA
    /// Notas de texto libre de un recordatorio.
    /// </summary>
    public class RecordatorioEnvioRespNota
    {
        public long IdRecordatorioEnvRespNota { get; set; }
        public long IdRecordatorioEnvioResp { get; set; }
        public string DescRecordatorioEnvioNota { get; set; }
    }
}
