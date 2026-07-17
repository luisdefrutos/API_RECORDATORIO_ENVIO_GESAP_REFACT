using System.Collections.Generic;
using RecordatorioEnvio.Domain.Entities;

namespace RecordatorioEnvio.Domain.Interfaces
{
    public interface IEstadoRepository
    {
        IEnumerable<EstadoRecEnvioRespuesta> GetAll();
        EstadoRecEnvioRespuesta GetById(long id);
    }
}
