using System;
using System.Collections.Generic;
using System.Data;
using RecordatorioEnvio.Domain.Entities;
using RecordatorioEnvio.Domain.Interfaces;
using RecordatorioEnvio.Infrastructure.Data;
using RecordatorioEnvio.Infrastructure.Logging;

namespace RecordatorioEnvio.Infrastructure.Repositories
{
    public class EstadoRepository : IEstadoRepository
    {
        private readonly OracleConnectionFactory _connectionFactory;

        public EstadoRepository(OracleConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IEnumerable<EstadoRecEnvioRespuesta> GetAll()
        {
            const string sql = "SELECT ID_ESTADO_REC_ENVIO_RESPUESTA, DESCRIPCION FROM GESAP.ESTADO_REC_ENVIO_RESPUESTA";
            var list = new List<EstadoRecEnvioRespuesta>();

            try
            {
                using (var content = _connectionFactory.CreateConnection())
                {
                    content.Open();
                    using (var cmd = content.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(new EstadoRecEnvioRespuesta
                                {
                                    IdEstadoRecEnvioRespuesta = Convert.ToInt64(reader["ID_ESTADO_REC_ENVIO_RESPUESTA"]),
                                    Descripcion = reader["DESCRIPCION"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex, "Error al obtener todos los estados", sql);
                throw;
            }

            return list;
        }

        public EstadoRecEnvioRespuesta GetById(long id)
        {
            const string sql = @"SELECT ID_ESTADO_REC_ENVIO_RESPUESTA, DESCRIPCION 
                                FROM GESAP.ESTADO_REC_ENVIO_RESPUESTA
                                WHERE ID_ESTADO_REC_ENVIO_RESPUESTA = :Id";
            try
            {
                using (var content = _connectionFactory.CreateConnection())
                {
                    content.Open();
                    using (var cmd = content.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        
                        var param = cmd.CreateParameter();
                        param.ParameterName = "Id";
                        param.Value = id;
                        cmd.Parameters.Add(param);
                        
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new EstadoRecEnvioRespuesta
                                {
                                    IdEstadoRecEnvioRespuesta = Convert.ToInt64(reader["ID_ESTADO_REC_ENVIO_RESPUESTA"]),
                                    Descripcion = reader["DESCRIPCION"].ToString()
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex, "Error al obtener estado por ID", sql);
                throw;
            }
            return null;
        }
    }
}
