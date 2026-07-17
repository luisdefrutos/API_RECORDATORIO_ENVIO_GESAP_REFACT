using System.Collections.Generic;
using RecordatorioEnvio.Domain.Entities;

namespace RecordatorioEnvio.Domain.Interfaces
{
    public interface IRecordatorioRepository
    {
        RecordatorioEnvioResp GetById(long id);

        RecordatorioEnvioResp GetByIdRecordatorioEnvio(long id);
        IEnumerable<RecordatorioEnvioResp> GetAll();
        void Add(RecordatorioEnvioResp entity);
        void Delete(long id);
        
        // Métodos específicos si se requieren
        IEnumerable<RecordatorioEnvioResp> GetByEstado(long idEstado);

        IEnumerable<RecordatorioEnvioDetalle> GetDetallesByRecordatorioEnvioId(long idRecordatorioEnvio);

        // Notas (RECORDATORIO_ENVIO_RESP_NOTA) — solo lectura; escritura va por SaveAll
        IEnumerable<RecordatorioEnvioRespNota> GetNotasByRespId(long idResp);

        /// <summary>
        /// Guarda formulario + items (MERGE) + notas (DELETE+INSERT) en UNA SOLA transacción Oracle.
        /// </summary>
        void SaveAll(RecordatorioEnvioResp entity, 
                     IEnumerable<RecordatorioEnvioResItem> items, 
                     IEnumerable<RecordatorioEnvioRespNota> notas);

        /// <summary>
        /// Obtiene el IDENTIFICADOR_REC_ENVIO de la tabla padre RECORDATORIO_ENVIO.
        /// </summary>
        RecordatorioEnvio.Domain.Entities.RecordatorioEnvio GetRecordatorioEnvioById(long idRecordatorioEnvio);
    }
}
