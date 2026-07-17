namespace RecordatorioEnvio.Application.DTOs
{
    /// <summary>
    /// DTO para el ítem del grid (checkbox de rechazo por equipo).
    /// El ID del item no viaja al frontend; lo calcula el backend al guardar.
    /// </summary>
    public class RecordatorioEnvioResItemDto
    {
        /// <summary>
        /// ID del ciclo de envío (identifica la fila del grid).
        /// </summary>
        public long IdRecordatorioCicloEnvio { get; set; }

        /// <summary>
        /// -1 = Aprobado (checkbox marcado), 0 = No aprobado.
        /// </summary>
        public int AprobadoCliente { get; set; }
    }
}
