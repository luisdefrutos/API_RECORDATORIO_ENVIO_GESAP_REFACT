using RecordatorioEnvio.Application.DTOs;
using RecordatorioEnvio.Domain.Entities;
using RecordatorioEnvio.Domain.Enums;
using RecordatorioEnvio.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RecordatorioEnvio.Application.Services
{
    /// <summary>
    /// Orquesta el flujo de negocio del Recordatorio de Envío.
    /// Responsabilidad única: coordinar repositorios, validación y transformación.
    /// La validación se delega en RecordatorioValidator (SRP).
    /// El mapeo se delega en RecordatorioMapper (SRP).
    /// El cifrado depende de IEncryptionService (DIP).
    /// Los logs dependen de IAuditoriaRepository (ISP).
    /// </summary>
    public class RecordatorioService
    {
        private readonly IRecordatorioRepository _recordatorioRepo;
        private readonly IEstadoRepository       _estadoRepo;
        private readonly IEncryptionService      _encryptionService;
        private readonly IAuditoriaRepository    _auditoriaRepo;

        public RecordatorioService(
            IRecordatorioRepository recordatorioRepo,
            IEstadoRepository estadoRepo,
            IEncryptionService encryptionService,
            IAuditoriaRepository auditoriaRepo)
        {
            _recordatorioRepo  = recordatorioRepo;
            _estadoRepo        = estadoRepo;
            _encryptionService = encryptionService;
            _auditoriaRepo     = auditoriaRepo;
        }

        public RecordatorioEnvioRespDto GetByIdRecordatorioEnvio(long id)
        {
            // Búsqueda por FK: localiza la respuesta asociada a un IdRecordatorioEnvio (tabla padre).
            var entity = _recordatorioRepo.GetByIdRecordatorioEnvio(id);
            if (entity == null) return null;

            var estado   = _estadoRepo.GetById(entity.IdEstadoRecEnvioRespuesta);
            var detalles = _recordatorioRepo.GetDetallesByRecordatorioEnvioId(entity.IdRecordatorioEnvio);
            var notas    = _recordatorioRepo.GetNotasByRespId(entity.IdRecordatorioEnvioResp);

            var dto = RecordatorioMapper.ToDto(entity, estado?.Descripcion, detalles, notas);

            // Se obtiene la entidad padre RECORDATORIO_ENVIO
            var padre = _recordatorioRepo.GetRecordatorioEnvioById(entity.IdRecordatorioEnvio);
            
            if (padre != null)
            {
                dto.IdentificadorRecEnvio = padre.IdentificadorRecEnvio;
                dto.RecordatorioEnvio = new RecordatorioEnvioDto
                {
                    IdRecordatorioEnvio = padre.IdRecordatorioEnvio,
                    IdEmplazamiento = padre.IdEmplazamiento,
                    IdTipoActuacion = padre.IdTipoActuacion,
                    FechaEnvioRecordatorio = padre.FechaEnvioRecordatorio,
                    TipoEnvio = padre.TipoEnvio,
                    Destinatario = padre.Destinatario,
                    CondicionesEcoCorreccion = padre.CondicionesEcoCorreccion,
                    EmailDestinatario = padre.EmailDestinatario,
                    IdentificadorRecEnvio = padre.IdentificadorRecEnvio,
                    IdCentro = padre.IdCentro,
                    EsEconomico = padre.EsEconomico,
                    CondicionesEcoGenerales = padre.CondicionesEcoGenerales,
                    
                };
            }

            return dto;
        }

        public IEnumerable<EstadoRecEnvioRespuesta> GetTodosEstados()
        {
            return _estadoRepo.GetAll();
        }

        public void Update(RecordatorioEnvioRespDto dto)
        {
            long id = dto.IdRecordatorioEnvioResp;
            if (id <= 0) throw new ArgumentException("ID Inválido");

            var dbEntity = _recordatorioRepo.GetById(id);
            if (dbEntity == null)
                throw new ArgumentException("El expediente especificado no existe o no es accesible.");

            if (dbEntity.IdRecordatorioEnvio != dto.IdRecordatorioEnvio)
                throw new ArgumentException("Violación de seguridad detectada: El expediente a actualizar no pertenece al recordatorio autorizado.");

            // Regla de inmutabilidad: solo se puede editar si el registro en BD está en estado Emitido.
            // PendienteRevision y Tramitado son estados finales — no se pueden modificar.
            if (dbEntity.IdEstadoRecEnvioRespuesta == (long)EstadosRespuesta.PendienteRevision ||
                dbEntity.IdEstadoRecEnvioRespuesta == (long)EstadosRespuesta.Tramitado)
            {
                throw new ArgumentException(
                    $"Operación denegada: El expediente se encuentra en estado " +
                    $"{dbEntity.IdEstadoRecEnvioRespuesta} y es inmutable.");
            }

            // Validaciones de formato (Notas, longitudes, etc.)
            var errors = RecordatorioValidator.Validate(dto);
            if (errors.Any()) throw new ArgumentException(string.Join(" | ", errors));

            var entity = RecordatorioMapper.ToEntity(dto, id);
            var items  = RecordatorioMapper.MapItems(dto.Items, id);
            var notas  = RecordatorioMapper.MapNotas(dto.Notas, id);

            _recordatorioRepo.SaveAll(entity, items, notas);
        }

        /// <summary>
        /// Valida las reglas de negocio complejas.
        /// Delega en RecordatorioValidator (SRP).
        /// </summary>
        public List<string> ValidateRules(RecordatorioEnvioRespDto dto)
        {
            return RecordatorioValidator.Validate(dto);
        }

        /// <summary>
        /// Obtiene los últimos N registros de auditoría.
        /// Delega en IAuditoriaRepository (ISP).
        /// </summary>
        public IEnumerable<LogEntryDto> GetRecentLogs(int count = 20)
        {
            var dt = _auditoriaRepo.GetRecentLogs(count);
            return RecordatorioMapper.ToLogDtoList(dt);
        }
    }
}
