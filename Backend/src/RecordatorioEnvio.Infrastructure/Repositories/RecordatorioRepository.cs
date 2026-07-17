using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Oracle.ManagedDataAccess.Client;
using RecordatorioEnvio.Domain.Entities;
using RecordatorioEnvio.Domain.Interfaces;
using RecordatorioEnvio.Infrastructure.Data;
using RecordatorioEnvio.Infrastructure.Logging;
using RecordatorioEnvio.Domain.Enums;

namespace RecordatorioEnvio.Infrastructure.Repositories
{
    public class RecordatorioRepository : IRecordatorioRepository, IAuditoriaRepository
    {
        private readonly OracleConnectionFactory _connectionFactory;

        #region SQL Queries
       

        private const string SqlGetById = @"
            SELECT * FROM GESAP.RECORDATORIO_ENVIO_RESP 
            WHERE ID_RECORDATORIO_ENVIO_RESP = :Id";

        private const string SqlGetByIdRecordatorioEnvio = @"
            SELECT * FROM GESAP.RECORDATORIO_ENVIO_RESP 
            WHERE ID_RECORDATORIO_ENVIO = :Id";

        private const string SqlGetAll = "SELECT * FROM GESAP.RECORDATORIO_ENVIO_RESP";

        private const string SqlInsertResp = @"
            INSERT INTO GESAP.RECORDATORIO_ENVIO_RESP (
                ID_RECORDATORIO_ENVIO_RESP, ID_RECORDATORIO_ENVIO, FECHA_EMISION, 
                EMPLAZAMIENTO_NOMBRE, EMPLAZAMIENTO_DIRECCION, 
                TITULAR_NIF, TITULAR_RAZON_SOCIAL, TITULAR_DIRECCION, TITULAR_PERSONA_CONTACTO, TITULAR_TELEFONO, TITULAR_EMAIL,
                GESTOR_PERSONA_CONTACTO, GESTOR_TELEFONO, GESTOR_EMAIL, GESTOR_NIF, GESTOR_RAZON_SOCIAL, GESTOR_DIRECCION,
                ID_ESTADO_REC_ENVIO_RESPUESTA,
                MODIFICADO_EMPLAZAMIENTO, MODIFICADO_TITULAR, MODIFICADO_GESTOR,
                FACTURAR_A, FACTURAR_A_OTRO_DESCRIPCION, FACTURAR_FORMA_PAGO, FACTURAR_CUENTA_BANCO,
                FECHA_RESPUESTA_CLIENTE
            ) VALUES (
                :Id, :IdRecEnvio, :FecEmision, 
                :EmpNombre, :EmpDireccion,
                :TitNif, :TitRazon, :TitDireccion, :TitContacto, :TitTelefono, :TitEmail,
                :GestContacto, :GestTelefono, :GestEmail, :GestNif, :GestRazon, :GestDireccion,
                :IdEstado,
                :ModEmp, :ModTit, :ModGest,
                :FacA, :FacOtro, :FacPago, :FacBanco,
                :FecResp
            )";

        private const string SqlUpdateResp = @"
            UPDATE GESAP.RECORDATORIO_ENVIO_RESP SET
                FECHA_RESPUESTA_CLIENTE       = :FecResp,
                EMPLAZAMIENTO_NOMBRE          = :EmpNombre,
                EMPLAZAMIENTO_DIRECCION       = :EmpDireccion,
                TITULAR_NIF                   = :TitNif,
                TITULAR_RAZON_SOCIAL          = :TitRazon,
                TITULAR_DIRECCION             = :TitDireccion,
                TITULAR_PERSONA_CONTACTO      = :TitContacto,
                TITULAR_TELEFONO              = :TitTelefono,
                TITULAR_EMAIL                 = :TitEmail,
                GESTOR_PERSONA_CONTACTO       = :GestContacto,
                GESTOR_TELEFONO               = :GestTelefono,
                GESTOR_EMAIL                  = :GestEmail,
                GESTOR_NIF                    = :GestNif,
                GESTOR_RAZON_SOCIAL           = :GestRazon,
                GESTOR_DIRECCION              = :GestDireccion,
                ID_ESTADO_REC_ENVIO_RESPUESTA = :IdEstado,
                MODIFICADO_EMPLAZAMIENTO      = :ModEmp,
                MODIFICADO_TITULAR            = :ModTit,
                MODIFICADO_GESTOR             = :ModGest,
                FACTURAR_A                    = :FacA,
                FACTURAR_A_OTRO_DESCRIPCION   = :FacOtro,
                FACTURAR_FORMA_PAGO           = :FacPago,
                FACTURAR_CUENTA_BANCO         = :FacBanco
            WHERE ID_RECORDATORIO_ENVIO_RESP = :Id";

        private const string SqlGetMaxItemId = "SELECT NVL(MAX(ID_RECORDATORIO_ENV_RESP_ITEM), 0) + 1 FROM GESAP.RECORDATORIO_ENVIO_RESP_ITEM";

        private const string SqlMergeItem = @"
            MERGE INTO GESAP.RECORDATORIO_ENVIO_RESP_ITEM T
            USING (SELECT :IdResp  AS ID_RESP, :IdCiclo AS ID_CICLO FROM DUAL) S
            ON (T.ID_RECORDATORIO_ENVIO_RESP = S.ID_RESP AND T.ID_RECORDATORIO_CICLO_ENVIO = S.ID_CICLO)
            WHEN MATCHED THEN
                UPDATE SET T.APROBADO_CLIENTE = :AprobadoUpd
            WHEN NOT MATCHED THEN
                INSERT (ID_RECORDATORIO_ENV_RESP_ITEM, ID_RECORDATORIO_ENVIO_RESP, ID_RECORDATORIO_CICLO_ENVIO, APROBADO_CLIENTE)
                VALUES (:NewId, :IdResp2, :IdCiclo2, :AprobadoIns)";

        private const string SqlDeleteNotas = "DELETE FROM GESAP.RECORDATORIO_ENVIO_RESP_NOTA WHERE ID_RECORDATORIO_ENVIO_RESP = :IdResp";

        private const string SqlGetMaxNotaId = "SELECT NVL(MAX(ID_RECORDATORIO_ENV_RESP_NOTA), 0) + 1 FROM GESAP.RECORDATORIO_ENVIO_RESP_NOTA";

        private const string SqlInsertNota = @"
            INSERT INTO GESAP.RECORDATORIO_ENVIO_RESP_NOTA (ID_RECORDATORIO_ENV_RESP_NOTA, ID_RECORDATORIO_ENVIO_RESP, DESC_RECORDATORIO_ENVIO_NOTA)
            VALUES (:IdNota, :IdResp, :DescNota)";

        private const string SqlDeleteResp = "DELETE FROM GESAP.RECORDATORIO_ENVIO_RESP WHERE ID_RECORDATORIO_ENVIO_RESP = :Id";





        private const string SqlGetDetalles = @"
            SELECT RECORDATORIO_ENVIO.ID_RECORDATORIO_ENVIO,
                   RECORDATORIO_CICLO_ENVIO.ID_RECORDATORIO_CICLO_ENVIO,
                   TIPO_ACTUACION.DESCRIPCION_TIPO_ACTUACION,
                   EQUIPO.Nombre_Equipo,
                   RECORDATORIO_CICLO.Fecha_Proxima_Actuacion,
                   SUBTIPO_ACTUACION.ID_SUBTIPO_ACTUACION,
                   SUBTIPO_ACTUACION.DESCRIPCION_SUBTIPO_ACTUACION AS TIPO_INSPECCION,
                   RECORDATORIO_CICLO_ENVIO.Precio_Actuacion_Primera_Rev AS PRECIO,
                   RECORDATORIO_CICLO_ENVIO.OTROS_COSTES,
                   (NVL(RECORDATORIO_CICLO_ENVIO.Precio_Actuacion_Primera_Rev, 0) + NVL(RECORDATORIO_CICLO_ENVIO.OTROS_COSTES, 0)) AS PRECIO_TOTAL,
                   RECORDATORIO_ENVIO_RESP_ITEM.APROBADO_CLIENTE,
                   CONFIGURACION_CENTRO.CENTRO_EMAIL
            FROM GESAP.RECORDATORIO_ENVIO
            JOIN GESAP.EMPLAZAMIENTO          ON RECORDATORIO_ENVIO.ID_EMPLAZAMIENTO = EMPLAZAMIENTO.Id_Emplazamiento
            JOIN GESAP.TIPO_ACTUACION         ON RECORDATORIO_ENVIO.Id_Tipo_Actuacion = TIPO_ACTUACION.Id_Tipo_Actuacion
            JOIN GESAP.RECORDATORIO_CICLO_ENVIO ON RECORDATORIO_ENVIO.Id_Recordatorio_Envio = RECORDATORIO_CICLO_ENVIO.Id_Recordatorio_Envio
            JOIN GESAP.RECORDATORIO_CICLO     ON RECORDATORIO_CICLO_ENVIO.Id_Recordatorio_Ciclo = RECORDATORIO_CICLO.Id_Recordatorio_Ciclo
            JOIN GESAP.EQUIPO_TIPO_ACTUACION  ON RECORDATORIO_CICLO.Id_Equipo_Tipo_Actuacion = EQUIPO_TIPO_ACTUACION.Id_Equipo_Tipo_Actuacion
            JOIN GESAP.EQUIPO                ON EQUIPO_TIPO_ACTUACION.Id_Equipo = EQUIPO.Id_Equipo
            JOIN GESAP.CONFIGURACION_CENTRO  ON  EQUIPO.ID_CENTRO_GESTOR_ACTUAL=CONFIGURACION_CENTRO.ID_CENTRO
            JOIN GESAP.SUBTIPO_ACTUACION      ON RECORDATORIO_CICLO.Id_Subtipo_Actuacion = SUBTIPO_ACTUACION.Id_Subtipo_Actuacion
            LEFT JOIN GESAP.RECORDATORIO_ENVIO_RESP
                   ON RECORDATORIO_ENVIO.ID_RECORDATORIO_ENVIO = RECORDATORIO_ENVIO_RESP.ID_RECORDATORIO_ENVIO
            LEFT JOIN GESAP.RECORDATORIO_ENVIO_RESP_ITEM
                   ON  RECORDATORIO_ENVIO_RESP.ID_RECORDATORIO_ENVIO_RESP = RECORDATORIO_ENVIO_RESP_ITEM.ID_RECORDATORIO_ENVIO_RESP
                   AND RECORDATORIO_ENVIO_RESP_ITEM.ID_RECORDATORIO_CICLO_ENVIO = RECORDATORIO_CICLO_ENVIO.ID_RECORDATORIO_CICLO_ENVIO
            WHERE RECORDATORIO_ENVIO.ID_RECORDATORIO_ENVIO = :Id
            ORDER BY RECORDATORIO_ENVIO.ID_RECORDATORIO_ENVIO, RECORDATORIO_CICLO.Fecha_Proxima_Actuacion";

        private const string SqlGetNotas = @"
            SELECT ID_RECORDATORIO_ENV_RESP_NOTA, ID_RECORDATORIO_ENVIO_RESP, DESC_RECORDATORIO_ENVIO_NOTA
            FROM GESAP.RECORDATORIO_ENVIO_RESP_NOTA
            WHERE ID_RECORDATORIO_ENVIO_RESP = :Id
            ORDER BY ID_RECORDATORIO_ENV_RESP_NOTA";

        private const string SqlGetRecentLogs = @"
            SELECT * FROM (
                SELECT LOG_ID, LOG_DATE, LOG_LEVEL, MESSAGE, EXCEPTION_MSG, IP_ADDRESS, ENDPOINT, SQL_QUERY
                FROM GESAP.RECORDATORIO_ENVIO_LOGS
                ORDER BY LOG_ID DESC
            ) WHERE ROWNUM <= :Count";
        #endregion

        public RecordatorioRepository(OracleConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

    
        public RecordatorioEnvioResp GetById(long id)
        {
            try
            {
                using (var conn = _connectionFactory.CreateConnection())
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = SqlGetById;
                        AddParameter(cmd, "Id", id);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read()) return MapReaderToEntity(reader);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex, "Error al obtener recordatorio por ID", SqlGetById);
                throw;
            }
            return null;
        }


        public RecordatorioEnvioResp GetByIdRecordatorioEnvio(long id)
        {
            try
            {
                using (var conn = _connectionFactory.CreateConnection())
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = SqlGetByIdRecordatorioEnvio;
                        AddParameter(cmd, "Id", id);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read()) return MapReaderToEntity(reader);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex, "Error al obtener recordatorio por ID", SqlGetById);
                throw;
            }
            return null;
        }
        public IEnumerable<RecordatorioEnvioResp> GetAll()
        {
            var list = new List<RecordatorioEnvioResp>();
            try
            {
                using (var conn = _connectionFactory.CreateConnection())
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = SqlGetAll;
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read()) list.Add(MapReaderToEntity(reader));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex, "Error al obtener todos los recordatorios", SqlGetAll);
                throw;
            }
            return list;
        }

        public void Add(RecordatorioEnvioResp entity)
        {
            using (var conn = _connectionFactory.CreateConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    ((OracleCommand)cmd).BindByName = true;
                    cmd.CommandText = SqlInsertResp;

                    AddParameter(cmd, "Id", entity.IdRecordatorioEnvioResp);
                    AddParameter(cmd, "IdRecEnvio", entity.IdRecordatorioEnvio);
                    AddParameter(cmd, "FecEmision", entity.FechaEmision);
                    AddParameter(cmd, "EmpNombre", entity.EmplazamientoNombre);
                    AddParameter(cmd, "EmpDireccion", entity.EmplazamientoDireccion);
                    AddParameter(cmd, "TitNif", entity.TitularNif);
                    AddParameter(cmd, "TitRazon", entity.TitularRazonSocial);
                    AddParameter(cmd, "TitDireccion", entity.TitularDireccion);
                    AddParameter(cmd, "TitContacto", entity.TitularPersonaContacto);
                    AddParameter(cmd, "TitTelefono", entity.TitularTelefono);
                    AddParameter(cmd, "TitEmail", entity.TitularEmail);
                    AddParameter(cmd, "GestContacto", entity.RepresentantePersonaContacto);
                    AddParameter(cmd, "GestTelefono", entity.RepresentanteTelefono);
                    AddParameter(cmd, "GestEmail", entity.RepresentanteEmail);
                    AddParameter(cmd, "GestNif", entity.RepresentanteNif);
                    AddParameter(cmd, "GestRazon", entity.RepresentanteRazonSocial);
                    AddParameter(cmd, "GestDireccion", entity.RepresentanteDireccion);
                    AddParameter(cmd, "IdEstado", entity.IdEstadoRecEnvioRespuesta);
                    AddParameter(cmd, "ModEmp", entity.ModificadoEmplazamiento);
                    AddParameter(cmd, "ModTit", entity.ModificadoTitular);
                    AddParameter(cmd, "ModGest", entity.ModificadoRepresentante);
                    AddParameter(cmd, "FacA", entity.FacturarA);
                    AddParameter(cmd, "FacOtro", entity.FacturarAOtroDescripcion);
                    AddParameter(cmd, "FacPago", entity.FacturarFormaPago);
                    AddParameter(cmd, "FacBanco", entity.FacturarCuentaBanco);
                    AddParameter(cmd, "FecResp", entity.FechaRespuestaCliente ?? DateTime.Now);

                    cmd.ExecuteNonQuery();
                }
            }
        }


        /// <summary>
        /// Guarda el formulario principal + items del grid + notas en UNA SOLA transacción Oracle.
        /// </summary>
        public void SaveAll(RecordatorioEnvioResp entity, IEnumerable<RecordatorioEnvioResItem> items, IEnumerable<RecordatorioEnvioRespNota> notas)
        {
            var itemsList = items?.ToList() ?? new List<RecordatorioEnvioResItem>();
            var notasList = notas?.ToList() ?? new List<RecordatorioEnvioRespNota>();
            long idResp = entity.IdRecordatorioEnvioResp;

            using (var conn = (OracleConnection)_connectionFactory.CreateConnection())
            {
                conn.Open();
                // Si existe un TransactionScope (transacción distribuida), no llamamos a BeginTransaction()
                // porque OracleConnection ya se enrola automáticamente y arrojaría excepción.
                OracleTransaction tx = null;
                if (System.Transactions.Transaction.Current == null)
                {
                    tx = conn.BeginTransaction();
                }
                    try
                    {
                        // 1. UPDATE
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tx;
                            ((OracleCommand)cmd).BindByName = true;
                            cmd.CommandText = SqlUpdateResp;

                            AddParameter(cmd, "FecResp",      entity.FechaRespuestaCliente ?? DateTime.Now);
                            AddParameter(cmd, "EmpNombre",    entity.EmplazamientoNombre);
                            AddParameter(cmd, "EmpDireccion", entity.EmplazamientoDireccion);
                            AddParameter(cmd, "TitNif",       entity.TitularNif);
                            AddParameter(cmd, "TitRazon",     entity.TitularRazonSocial);
                            AddParameter(cmd, "TitDireccion", entity.TitularDireccion);
                            AddParameter(cmd, "TitContacto",  entity.TitularPersonaContacto);
                            AddParameter(cmd, "TitTelefono",  entity.TitularTelefono);
                            AddParameter(cmd, "TitEmail",     entity.TitularEmail);
                            AddParameter(cmd, "GestContacto", entity.RepresentantePersonaContacto);
                            AddParameter(cmd, "GestTelefono", entity.RepresentanteTelefono);
                            AddParameter(cmd, "GestEmail",    entity.RepresentanteEmail);
                            AddParameter(cmd, "GestNif",      entity.RepresentanteNif);
                            AddParameter(cmd, "GestRazon",    entity.RepresentanteRazonSocial);
                            AddParameter(cmd, "GestDireccion",entity.RepresentanteDireccion);
                            AddParameter(cmd, "IdEstado",     (long)EstadosRespuesta.PendienteRevision);
                            AddParameter(cmd, "ModEmp",       entity.ModificadoEmplazamiento);
                            AddParameter(cmd, "ModTit",       entity.ModificadoTitular);
                            AddParameter(cmd, "ModGest",      entity.ModificadoRepresentante);
                            AddParameter(cmd, "FacA",         entity.FacturarA);
                            AddParameter(cmd, "FacOtro",      entity.FacturarAOtroDescripcion);
                            AddParameter(cmd, "FacPago",      entity.FacturarFormaPago);
                            AddParameter(cmd, "FacBanco",     entity.FacturarCuentaBanco);
                            AddParameter(cmd, "Id",           idResp);
                            cmd.ExecuteNonQuery();
                        }

                        // 2. MERGE items
                        foreach (var item in itemsList)
                        {
                            long newItemId;
                            using (var cmdMax = conn.CreateCommand())
                            {
                                cmdMax.Transaction = tx;
                                cmdMax.CommandText = SqlGetMaxItemId;
                                newItemId = Convert.ToInt64(cmdMax.ExecuteScalar());
                            }

                            using (var cmdMerge = conn.CreateCommand())
                            {
                                cmdMerge.Transaction = tx;
                                ((OracleCommand)cmdMerge).BindByName = true;
                                cmdMerge.CommandText = SqlMergeItem;
                                AddParameter(cmdMerge, "IdResp",      idResp);
                                AddParameter(cmdMerge, "IdCiclo",     item.IdRecordatorioCicloEnvio);
                                AddParameter(cmdMerge, "AprobadoUpd", item.AprobadoCliente);
                                AddParameter(cmdMerge, "NewId",       newItemId);
                                AddParameter(cmdMerge, "IdResp2",     idResp);
                                AddParameter(cmdMerge, "IdCiclo2",    item.IdRecordatorioCicloEnvio);
                                AddParameter(cmdMerge, "AprobadoIns", item.AprobadoCliente);
                                cmdMerge.ExecuteNonQuery();
                            }
                        }

                        // 3. DELETE notas
                        using (var cmdDel = conn.CreateCommand())
                        {
                            cmdDel.Transaction = tx;
                            ((OracleCommand)cmdDel).BindByName = true;
                            cmdDel.CommandText = SqlDeleteNotas;
                            AddParameter(cmdDel, "IdResp", idResp);
                            cmdDel.ExecuteNonQuery();
                        }

                        // 4. INSERT notas
                        using (var cmdNota = conn.CreateCommand())
                        {
                            cmdNota.Transaction = tx;
                            ((OracleCommand)cmdNota).BindByName = true;
                            cmdNota.CommandText = SqlInsertNota;
                            foreach (var nota in notasList)
                            {
                                long newNotaId;
                                using (var cmdMaxNota = conn.CreateCommand())
                                {
                                    cmdMaxNota.Transaction = tx;
                                    cmdMaxNota.CommandText = SqlGetMaxNotaId;
                                    newNotaId = Convert.ToInt64(cmdMaxNota.ExecuteScalar());
                                }
                                cmdNota.Parameters.Clear();
                                AddParameter(cmdNota, "IdNota", newNotaId);
                                AddParameter(cmdNota, "IdResp", idResp);
                                AddParameter(cmdNota, "DescNota",   nota.DescRecordatorioEnvioNota);
                                cmdNota.ExecuteNonQuery();
                            }
                        }

                        if (tx != null) tx.Commit();
                    }
                    catch (Exception ex)
                    {
                        if (tx != null) tx.Rollback();
                        LogHelper.Error(ex, "Fallo en transacción SaveAll (Rollback ejecutado)", "SaveAll Transaction");
                        throw;
                    }
                    finally
                    {
                        if (tx != null) tx.Dispose();
                    }
            }
        }


        public void Delete(long id)
        {
            using (var conn = _connectionFactory.CreateConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = SqlDeleteResp;
                    AddParameter(cmd, "Id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public IEnumerable<RecordatorioEnvioResp> GetByEstado(long idEstado)
        {
             return new List<RecordatorioEnvioResp>();
        }

        public IEnumerable<RecordatorioEnvioDetalle> GetDetallesByRecordatorioEnvioId(long idRecordatorioEnvio)
        {
            var list = new List<RecordatorioEnvioDetalle>();
            using (var conn = _connectionFactory.CreateConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = SqlGetDetalles;

                    AddParameter(cmd, "Id", idRecordatorioEnvio);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new RecordatorioEnvioDetalle
                            {
                                IdRecordatorioEnvio       = Convert.ToInt32(reader["ID_RECORDATORIO_ENVIO"]),
                                IdRecordatorioCicloEnvio  = Convert.ToInt64(reader["ID_RECORDATORIO_CICLO_ENVIO"]),
                                DescripcionTipoActuacion  = reader["DESCRIPCION_TIPO_ACTUACION"] as string,
                                NombreEquipo              = reader["Nombre_Equipo"] as string,
                                FechaProximaActuacion     = reader["Fecha_Proxima_Actuacion"] as DateTime?,
                                IdSubtipoActuacion        = GetNullableInt(reader, "ID_SUBTIPO_ACTUACION"),
                                TipoInspeccion            = reader["TIPO_INSPECCION"] as string,
                                PrecioActuacionPrimeraRev = GetNullableDecimal(reader, "PRECIO"),
                                OtrosCostes               = GetNullableDecimal(reader, "OTROS_COSTES"),
                                PrecioTotal               = GetNullableDecimal(reader, "PRECIO_TOTAL"),
                                CentroEmail               =reader["CENTRO_EMAIL"] as string,
                                AprobadoCliente           = reader["APROBADO_CLIENTE"] == DBNull.Value
                                                            ? (int?)null
                                                            : Convert.ToInt32(reader["APROBADO_CLIENTE"])
                            });
                        }
                    }
                }
            }
            return list;
        }


        // --- NOTAS (RECORDATORIO_ENVIO_RESP_NOTA) ---
 
        public IEnumerable<RecordatorioEnvioRespNota> GetNotasByRespId(long idResp)
        {
            var list = new List<RecordatorioEnvioRespNota>();
            using (var conn = (OracleConnection)_connectionFactory.CreateConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = SqlGetNotas;
                    AddParameter(cmd, "Id", idResp);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(MapReaderToNota(reader));
                        }
                    }
                }
            }
            return list;
        }

        // --- AUDITORÍA DE LOGS ---
        public DataTable GetRecentLogs(int count = 20)
        {
            var dt = new DataTable();
            using (var conn = _connectionFactory.CreateConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = SqlGetRecentLogs;
                    AddParameter(cmd, "Count", count);
                    
                    using (var adapter = new OracleDataAdapter((OracleCommand)cmd))
                    {
                        adapter.Fill(dt);
                    }
                }
            }
            return dt;
        }


        private RecordatorioEnvioRespNota MapReaderToNota(IDataReader reader)
        {
            return new RecordatorioEnvioRespNota
            {
                IdRecordatorioEnvRespNota = Convert.ToInt64(reader["ID_RECORDATORIO_ENV_RESP_NOTA"]),
                IdRecordatorioEnvioResp = Convert.ToInt64(reader["ID_RECORDATORIO_ENVIO_RESP"]),
                DescRecordatorioEnvioNota = reader["DESC_RECORDATORIO_ENVIO_NOTA"] as string
            };
        }

        private void AddParameter(IDbCommand cmd, string name, object value)
        {
            var param = cmd.CreateParameter();
            param.ParameterName = name;
            param.Value = value ?? DBNull.Value;

            if (value != null && value.GetType().IsEnum)
                param.Value = Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType()));

            cmd.Parameters.Add(param);
        }

        // Helper auxiliar para Decimales
        private decimal? GetNullableDecimal(IDataReader reader, string columnName)
        {
            var val = reader[columnName];
            return (val == null || val == DBNull.Value) ? null : (decimal?)Convert.ToDecimal(val);
        }

        private long GetLong(IDataReader reader, string columnName)
        {
            var val = reader[columnName];
            return (val == null || val == DBNull.Value) ? 0 : Convert.ToInt64(val);
        }

        private RecordatorioEnvioResp MapReaderToEntity(IDataReader reader)
        {
            return new RecordatorioEnvioResp
            {
                IdRecordatorioEnvioResp  = GetLong(reader, "ID_RECORDATORIO_ENVIO_RESP"),
                IdRecordatorioEnvio       = GetLong(reader, "ID_RECORDATORIO_ENVIO"),
                FechaEmision              = reader["FECHA_EMISION"] as DateTime?,
                FechaRespuestaCliente     = reader["FECHA_RESPUESTA_CLIENTE"] as DateTime?,
                FechaTramitacion          = reader["FECHA_TRAMITACION"] as DateTime?,

                EmplazamientoNombre       = reader["EMPLAZAMIENTO_NOMBRE"] as string,
                EmplazamientoDireccion    = reader["EMPLAZAMIENTO_DIRECCION"] as string,

                TitularNif                = reader["TITULAR_NIF"] as string,
                TitularRazonSocial        = reader["TITULAR_RAZON_SOCIAL"] as string,
                TitularDireccion          = reader["TITULAR_DIRECCION"] as string,
                TitularPersonaContacto    = reader["TITULAR_PERSONA_CONTACTO"] as string,
                TitularTelefono           = reader["TITULAR_TELEFONO"] as string,
                TitularEmail              = reader["TITULAR_EMAIL"] as string,

                RepresentantePersonaContacto = reader["GESTOR_PERSONA_CONTACTO"] as string,
                RepresentanteTelefono        = reader["GESTOR_TELEFONO"] as string,
                RepresentanteEmail           = reader["GESTOR_EMAIL"] as string,
                RepresentanteNif             = reader["GESTOR_NIF"] as string,
                RepresentanteRazonSocial     = reader["GESTOR_RAZON_SOCIAL"] as string,
                RepresentanteDireccion       = reader["GESTOR_DIRECCION"] as string,

                IdEstadoRecEnvioRespuesta = GetLong(reader, "ID_ESTADO_REC_ENVIO_RESPUESTA"),

                ModificadoEmplazamiento  = GetNullableInt(reader, "MODIFICADO_EMPLAZAMIENTO"),
                ModificadoTitular        = GetNullableInt(reader, "MODIFICADO_TITULAR"),
                ModificadoRepresentante  = GetNullableInt(reader, "MODIFICADO_GESTOR"),

                FacturarA                = reader["FACTURAR_A"] as string,
                FacturarAOtroDescripcion = reader["FACTURAR_A_OTRO_DESCRIPCION"] as string,
                FacturarFormaPago        = reader["FACTURAR_FORMA_PAGO"] as string,
                FacturarCuentaBanco      = reader["FACTURAR_CUENTA_BANCO"] as string
            };
        }

        private string GetSafeString(IDataReader reader, string columnName)
        {
            try {
                var val = reader[columnName];
                return val == DBNull.Value ? null : val as string;
            } catch (IndexOutOfRangeException) { return null; }
        }

        private int? GetNullableInt(IDataReader reader, string columnName)
        {
            var val = reader[columnName];
            return (val == null || val == DBNull.Value) ? null : (int?)Convert.ToInt32(val);
        }

        /// <summary>
        /// Obtiene el IDENTIFICADOR_REC_ENVIO de la tabla padre RECORDATORIO_ENVIO.
        /// </summary>
        public RecordatorioEnvio.Domain.Entities.RecordatorioEnvio GetRecordatorioEnvioById(long idRecordatorioEnvio)
        {
            try
            {
                using (var conn = _connectionFactory.CreateConnection())
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            SELECT ID_RECORDATORIO_ENVIO, ID_EMPLAZAMIENTO, ID_TIPO_ACTUACION, FECHA_ENVIO_RECORDATORIO, 
                                   TIPO_ENVIO, DESTINATARIO, CONDICIONES_ECO_CORRECCION, EMAIL_DESTINATARIO, 
                                   IDENTIFICADOR_REC_ENVIO, ID_CENTRO, ES_ECONOMICO, CONDICIONES_ECO_GENERALES 
                            FROM GESAP.RECORDATORIO_ENVIO 
                            WHERE ID_RECORDATORIO_ENVIO = :Id";
                        AddParameter(cmd, "Id", idRecordatorioEnvio);
                        
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new RecordatorioEnvio.Domain.Entities.RecordatorioEnvio
                                {
                                    IdRecordatorioEnvio = Convert.ToInt64(reader["ID_RECORDATORIO_ENVIO"]),
                                    IdEmplazamiento = reader["ID_EMPLAZAMIENTO"] != DBNull.Value ? Convert.ToInt64(reader["ID_EMPLAZAMIENTO"]) : (long?)null,
                                    IdTipoActuacion = reader["ID_TIPO_ACTUACION"] != DBNull.Value ? Convert.ToInt64(reader["ID_TIPO_ACTUACION"]) : (long?)null,
                                    FechaEnvioRecordatorio = reader["FECHA_ENVIO_RECORDATORIO"] != DBNull.Value ? Convert.ToDateTime(reader["FECHA_ENVIO_RECORDATORIO"]) : (DateTime?)null,
                                    TipoEnvio = reader["TIPO_ENVIO"] != DBNull.Value ? reader["TIPO_ENVIO"].ToString() : null,
                                    Destinatario = reader["DESTINATARIO"] != DBNull.Value ? reader["DESTINATARIO"].ToString() : null,
                                    CondicionesEcoCorreccion = reader["CONDICIONES_ECO_CORRECCION"] != DBNull.Value ? reader["CONDICIONES_ECO_CORRECCION"].ToString() : null,
                                    EmailDestinatario = reader["EMAIL_DESTINATARIO"] != DBNull.Value ? reader["EMAIL_DESTINATARIO"].ToString() : null,
                                    IdentificadorRecEnvio = reader["IDENTIFICADOR_REC_ENVIO"] != DBNull.Value ? reader["IDENTIFICADOR_REC_ENVIO"].ToString() : null,
                                    IdCentro = reader["ID_CENTRO"] != DBNull.Value ? Convert.ToInt64(reader["ID_CENTRO"]) : (long?)null,
                                    EsEconomico = reader["ES_ECONOMICO"] != DBNull.Value ? Convert.ToInt32(reader["ES_ECONOMICO"]) : (int?)null,
                                    CondicionesEcoGenerales = reader["CONDICIONES_ECO_GENERALES"] != DBNull.Value ? reader["CONDICIONES_ECO_GENERALES"].ToString() : null
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex, "Error al obtener RECORDATORIO_ENVIO", "GetRecordatorioEnvioById");
            }
            return null;
        }

       
    }
}
