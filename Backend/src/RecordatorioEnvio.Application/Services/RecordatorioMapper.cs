using RecordatorioEnvio.Application.DTOs;
using RecordatorioEnvio.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RecordatorioEnvio.Application.Services
{
    /// <summary>
    /// Responsabilidad única: transformar entre entidades de dominio y DTOs.
    /// Extraído de RecordatorioService para cumplir SRP.
    /// </summary>
    internal static class RecordatorioMapper
    {
        internal static RecordatorioEnvioRespDto ToDto(
            RecordatorioEnvioResp entity,
            string estadoDesc,
            IEnumerable<RecordatorioEnvioDetalle> detalles = null,
            IEnumerable<RecordatorioEnvioRespNota> notas = null)
        {
            var dto = new RecordatorioEnvioRespDto
            {
                IdRecordatorioEnvioResp = entity.IdRecordatorioEnvioResp,
                IdRecordatorioEnvio = entity.IdRecordatorioEnvio,
                FechaEmision = entity.FechaEmision,
                FechaRespuestaCliente = entity.FechaRespuestaCliente,
                FechaTramitacion = entity.FechaTramitacion,

                EmplazamientoNombre = entity.EmplazamientoNombre,
                EmplazamientoDireccion = entity.EmplazamientoDireccion,

                TitularNif = entity.TitularNif,
                TitularRazonSocial = entity.TitularRazonSocial,
                TitularDireccion = entity.TitularDireccion,
                TitularPersonaContacto = entity.TitularPersonaContacto,
                TitularTelefono = entity.TitularTelefono,
                TitularEmail = entity.TitularEmail,

                RepresentantePersonaContacto = entity.RepresentantePersonaContacto,
                RepresentanteTelefono = entity.RepresentanteTelefono,
                RepresentanteEmail = entity.RepresentanteEmail,
                RepresentanteNif = entity.RepresentanteNif,
                RepresentanteRazonSocial = entity.RepresentanteRazonSocial,
                RepresentanteDireccion = entity.RepresentanteDireccion,

                IdEstadoRecEnvioRespuesta = entity.IdEstadoRecEnvioRespuesta,
                EstadoDescripcion = estadoDesc,

                ModificadoEmplazamiento = entity.ModificadoEmplazamiento,
                ModificadoTitular = entity.ModificadoTitular,
                ModificadoRepresentante = entity.ModificadoRepresentante,

                FacturarA = entity.FacturarA,
                FacturarAOtroDescripcion = entity.FacturarAOtroDescripcion,
                FacturarFormaPago = entity.FacturarFormaPago,
                FacturarCuentaBanco = entity.FacturarCuentaBanco,

                Detalles = new List<RecordatorioEnvioDetalleDto>()
            };

            if (detalles != null)
            {
                dto.Detalles = detalles.Select(d => new RecordatorioEnvioDetalleDto
                {
                    IdRecordatorioEnvio      = d.IdRecordatorioEnvio,
                    IdRecordatorioCicloEnvio = d.IdRecordatorioCicloEnvio,
                    DescripcionTipoActuacion = d.DescripcionTipoActuacion,
                    NombreEquipo             = d.NombreEquipo,
                    FechaProximaActuacion    = d.FechaProximaActuacion,
                    IdSubtipoActuacion       = d.IdSubtipoActuacion,
                    TipoInspeccion           = d.TipoInspeccion,
                    PrecioActuacionPrimeraRev = d.PrecioActuacionPrimeraRev,
                    OtrosCostes              = d.OtrosCostes,
                    PrecioTotal              = d.PrecioTotal,
                    AprobadoCliente          = d.AprobadoCliente,
                    CentroEmail              = d.CentroEmail
                }).ToList();
            }

            if (notas != null)
            {
                dto.Notas = notas.Select(n => new RecordatorioEnvioRespNotaDto
                {
                    DescRecordatorioEnvioNota = n.DescRecordatorioEnvioNota
                }).ToList();
            }

            return dto;
        }

        internal static RecordatorioEnvioResp ToEntity(RecordatorioEnvioRespDto dto, long id)
        {
            return new RecordatorioEnvioResp
            {
                IdRecordatorioEnvioResp = id,
                IdRecordatorioEnvio = dto.IdRecordatorioEnvio,
                FechaEmision = dto.FechaEmision,
                FechaRespuestaCliente = dto.FechaRespuestaCliente,
                FechaTramitacion = dto.FechaTramitacion,

                EmplazamientoNombre = dto.EmplazamientoNombre,
                EmplazamientoDireccion = dto.EmplazamientoDireccion,

                TitularNif = dto.TitularNif,
                TitularRazonSocial = dto.TitularRazonSocial,
                TitularDireccion = dto.TitularDireccion,
                TitularPersonaContacto = dto.TitularPersonaContacto,
                TitularTelefono = dto.TitularTelefono,
                TitularEmail = dto.TitularEmail,

                RepresentantePersonaContacto = dto.RepresentantePersonaContacto,
                RepresentanteTelefono = dto.RepresentanteTelefono,
                RepresentanteEmail = dto.RepresentanteEmail,
                RepresentanteNif = dto.RepresentanteNif,
                RepresentanteRazonSocial = dto.RepresentanteRazonSocial,
                RepresentanteDireccion = dto.RepresentanteDireccion,

                IdEstadoRecEnvioRespuesta = dto.IdEstadoRecEnvioRespuesta,

                ModificadoEmplazamiento = dto.ModificadoEmplazamiento,
                ModificadoTitular = dto.ModificadoTitular,
                ModificadoRepresentante = dto.ModificadoRepresentante,

                FacturarA = dto.FacturarA,
                FacturarAOtroDescripcion = dto.FacturarAOtroDescripcion,
                FacturarFormaPago = dto.FacturarFormaPago,
                FacturarCuentaBanco = dto.FacturarCuentaBanco
            };
        }

        internal static List<RecordatorioEnvioResItem> MapItems(IEnumerable<RecordatorioEnvioResItemDto> dtos, long idResp)
        {
            return (dtos ?? new List<RecordatorioEnvioResItemDto>())
                .Select(i => new RecordatorioEnvioResItem
                {
                    IdRecordatorioEnvioResp  = idResp,
                    IdRecordatorioCicloEnvio = i.IdRecordatorioCicloEnvio,
                    AprobadoCliente          = i.AprobadoCliente
                }).ToList();
        }

        internal static List<RecordatorioEnvioRespNota> MapNotas(IEnumerable<RecordatorioEnvioRespNotaDto> dtos, long idResp)
        {
            return (dtos ?? new List<RecordatorioEnvioRespNotaDto>())
                .Where(n => !string.IsNullOrWhiteSpace(n.DescRecordatorioEnvioNota))
                .Select(n => new RecordatorioEnvioRespNota
                {
                    IdRecordatorioEnvioResp   = idResp,
                    DescRecordatorioEnvioNota = n.DescRecordatorioEnvioNota.Trim()
                }).ToList();
        }

        internal static List<LogEntryDto> ToLogDtoList(System.Data.DataTable dt)
        {
            var logs = new List<LogEntryDto>();
            if (dt == null) return logs;

            foreach (System.Data.DataRow row in dt.Rows)
            {
                logs.Add(new LogEntryDto
                {
                    LogId        = Convert.ToInt64(row["LOG_ID"]),
                    LogDate      = Convert.ToDateTime(row["LOG_DATE"]),
                    LogLevel     = row["LOG_LEVEL"] as string,
                    Message      = row["MESSAGE"] as string,
                    ExceptionMsg = row["EXCEPTION_MSG"] as string,
                    IpAddress    = row["IP_ADDRESS"] as string,
                    Endpoint     = row["ENDPOINT"] as string,
                    SqlQuery     = row["SQL_QUERY"] as string
                });
            }
            return logs;
        }
    }
}
