using System.Data;

namespace RecordatorioEnvio.Domain.Interfaces
{
    /// <summary>
    /// Abstracción para consultas de auditoría de logs.
    /// Separada de IRecordatorioRepository siguiendo el principio
    /// de Segregación de Interfaces (ISP): los consumidores que solo
    /// necesitan leer logs no deben depender del repositorio principal.
    /// </summary>
    public interface IAuditoriaRepository
    {
        /// <summary>Obtiene los últimos N registros de auditoría.</summary>
        DataTable GetRecentLogs(int count = 20);
    }
}
