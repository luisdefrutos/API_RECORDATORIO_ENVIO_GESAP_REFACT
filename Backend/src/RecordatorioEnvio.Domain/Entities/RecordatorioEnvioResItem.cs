namespace RecordatorioEnvio.Domain.Entities
{
    /// <summary>
    /// Representa un ítem del grid de equipos, indicando si fue aprobado por el cliente.
    /// Tabla: RECORDATORIO_ENVIO_RESP_ITEM
    /// </summary>
    public class RecordatorioEnvioResItem
    {
        public long IdRecordatorioEnvRespItem { get; set; }
        public long IdRecordatorioEnvioResp { get; set; }
        public long IdRecordatorioCicloEnvio { get; set; }

        /// <summary>
        /// -1 = Aprobado (marcado), 0 = No aprobado
        /// </summary>
        public int AprobadoCliente { get; set; }
    }
}
