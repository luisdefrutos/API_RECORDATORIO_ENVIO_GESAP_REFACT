using System;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using RecordatorioEnvio.Domain.Entities;
using RecordatorioEnvio.Domain.Interfaces;
using RecordatorioEnvio.Infrastructure.Data;

namespace RecordatorioEnvio.Infrastructure.Repositories
{
    public class SysConfiguracionRepository : ISysConfiguracionRepository
    {
        private readonly OracleConnectionFactory _connectionFactory;

        public SysConfiguracionRepository(OracleConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public SysConfiguracion GetConfiguracion()
        {
            using (var conn = _connectionFactory.CreateConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM (SELECT * FROM GESAP.SYS_CONFIGURACION ORDER BY ID_SYS_CONFIGURACION DESC) WHERE ROWNUM = 1";
                    cmd.CommandType = CommandType.Text;

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new SysConfiguracion
                            {
                                IdSysConfiguracion = reader["ID_SYS_CONFIGURACION"] != DBNull.Value ? Convert.ToInt64(reader["ID_SYS_CONFIGURACION"]) : 0,
                                EsBbddReal = reader["ES_BBDD_REAL"] != DBNull.Value ? Convert.ToInt32(reader["ES_BBDD_REAL"]) : 0,
                                EmailNotificacionesDireccion = reader["EMAIL_NOTIFICACIONES_DIRECCION"]?.ToString(),
                                EmailNotificacionesUsuario = reader["EMAIL_NOTIFICACIONES_USUARIO"]?.ToString(),
                                EmailNotificacionesPassword = reader["EMAIL_NOTIFICACIONES_PASSWORD"]?.ToString(),
                                ServidorCorreoDireccion = reader["SERVIDOR_CORREO_DIRECCION"]?.ToString(),
                                ServidorCorreoPuerto = reader["SERVIDOR_CORREO_PUERTO"]?.ToString(),
                                EmailLog = reader["EMAIL_LOG"]?.ToString(),
                                EmailEnvioGir= reader["EMAIL_ENVIO_GIR"]?.ToString(),
                                EmailSsl = reader["EMAIL_SSL"] != DBNull.Value ? Convert.ToInt32(reader["EMAIL_SSL"]) : 0
                            };
                        }
                    }
                }
            }
            return null;
        }
    }
}
