using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using System.Data;
using RecordatorioEnvio.Infrastructure.Data;
using Oracle.ManagedDataAccess.Client;

using System.Configuration;

namespace RecordatorioEnvio.Infrastructure.Logging
{
    public static class LogHelper
    {
        private enum LogLevel { DEBUG = 0, INFO = 1, WARN = 2, ERROR = 3, FATAL = 4, NONE = 5 }
        private static readonly object _lock = new object();

        private static readonly string _separator    = new string('#', 50);
        private static readonly string _separatorMin = new string('-', 50);

        private static int GetMaxDaysLog()
        {
            if (int.TryParse(ConfigurationManager.AppSettings["LogRetentionDays"], out int days))
            {
                return days;
            }
            return 30; // Default fallback
        }

        // ─────────────────────────────────────────────────────────────────────
        // INTERRUPTOR MAESTRO DE BD
        // Controla si se escribe en BD Oracle. Si no existe el parámetro en
        // Web.config, por defecto está HABILITADO (retrocompatibilidad).
        // ─────────────────────────────────────────────────────────────────────
        private static bool IsDbEnabled()
        {
            string val = ConfigurationManager.AppSettings["Log_EnableDb"];
            if (string.IsNullOrEmpty(val)) return true; // Si no existe, BD activa (retrocompatible)
            return !val.Equals("false", StringComparison.OrdinalIgnoreCase);
        }

        // ─────────────────────────────────────────────────────────────────────
        // API PÚBLICA
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Log informativo (nivel INFO). Solo va a fichero TXT si Audit_LogLevel lo permite.
        /// </summary>
        public static void Info(string message, string corrId = null, long? recordId = null)
        {
            if (ShouldLog("INFO", false)) WriteToFile(message, "INFO", corrId, recordId);
            if (ShouldLog("INFO", true))  LogToDb(message, "INFO", null, null, corrId: corrId, recordId: recordId);
        }

        /// <summary>
        /// Log de advertencia (nivel WARN). Va a fichero TXT con separadores ####.
        /// Si Audit_DbLogLevel lo permite, también a BD Oracle.
        /// </summary>
        public static void Warn(string message, string sqlQuery = null, string corrId = null, long? recordId = null)
        {
            if (ShouldLog("WARN", false)) WriteToFile(message, "WARN", corrId, recordId, sqlQuery: sqlQuery);
            if (ShouldLog("WARN", true))  LogToDb(message, "WARN", null, sqlQuery, corrId: corrId, recordId: recordId);
        }

        /// <summary>
        /// Log de error (nivel ERROR). Va a fichero TXT con separadores #### y bloque estructurado
        /// (CONTEXT / EXCEPTION / SQL / STACKTRACE). Si Audit_DbLogLevel lo permite, también a BD.
        /// Incluye deduplicación: si la excepción ya fue logueada al burbujear, no se repite en BD.
        /// </summary>
        public static void Error(Exception ex, string context = "", string sqlQuery = null, string corrId = null, long? recordId = null)
        {
            // DEDUPLICACIÓN: Si la excepción ya fue logueada, no repetimos en BD
            bool alreadyLogged = ex != null && ex.Data.Contains("LoggedByLogHelper");

            string contextMsg = $"ERROR {context}: {ex?.Message}";

            // Siempre escribimos al fichero si el nivel lo permite (con ex estructurado)
            if (ShouldLog("ERROR", false))
            {
                WriteToFile(contextMsg, "ERROR", corrId, recordId, ex: ex, sqlQuery: sqlQuery);
            }

            if (!alreadyLogged && ShouldLog("ERROR", true))
            {
                LogToDb(contextMsg, "ERROR", ex, sqlQuery, corrId: corrId, recordId: recordId);

                // Marcamos como logueada para evitar duplicados en el bubble-up
                if (ex != null) try { ex.Data["LoggedByLogHelper"] = true; } catch { }
            }
        }

        /// <summary>
        /// Log genérico por tipo. Enviar "INFO", "WARN", "ERROR", etc.
        /// Los niveles WARN/ERROR/FATAL generan separadores #### en el TXT.
        /// </summary>
        public static void Log(string message, string type = "INFO", string corrId = null, long? recordId = null)
        {
            try
            {
                if (!ShouldLog(type, false)) return;

                WriteToFile(message, type, corrId, recordId);

                if (ShouldLog(type, true))
                {
                    LogToDb(message, type, null, null, corrId: corrId, recordId: recordId);
                }
            }
            catch (Exception ex)
            {
                try { System.Diagnostics.EventLog.WriteEntry("Application", "GESAP Log Error: " + ex.Message, System.Diagnostics.EventLogEntryType.Error); } catch { }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // CONTROL DE NIVEL
        // ─────────────────────────────────────────────────────────────────────

        private static bool ShouldLog(string levelStr, bool isDb)
        {
            try
            {
                // 0. Interruptor maestro de BD (solo aplica cuando isDb=true)
                if (isDb && !IsDbEnabled()) return false;

                // 1. Obtener nivel solicitado
                if (!Enum.TryParse(levelStr.ToUpper(), out LogLevel requestedLevel))
                    requestedLevel = LogLevel.INFO;

                // 2. Obtener nivel configurado
                string configKey = isDb ? "Audit_DbLogLevel" : "Audit_LogLevel";
                string configVal = ConfigurationManager.AppSettings[configKey];

                // Fallback: si no hay nivel de BD específico, usamos el maestro de TXT
                if (isDb && string.IsNullOrEmpty(configVal))
                {
                    configVal = ConfigurationManager.AppSettings["Audit_LogLevel"];
                }

                if (string.IsNullOrEmpty(configVal)) return true; // Por defecto logueamos todo

                if (!Enum.TryParse(configVal.ToUpper(), out LogLevel minLevel))
                    minLevel = LogLevel.INFO;

                return requestedLevel >= minLevel;
            }
            catch
            {
                return true; // En caso de duda, logueamos
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // ESCRITURA EN FICHERO TXT (enriquecida)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Escribe una entrada en el fichero TXT diario.
        /// — INFO/DEBUG: línea única enriquecida.
        /// — WARN/ERROR/FATAL: bloque estructurado con separadores ##########.
        /// </summary>
        private static void WriteToFile(string message, string type, string corrId = null, long? recordId = null,
                                         Exception ex = null, string sqlQuery = null)
        {
            try
            {
                string folderPath = HostingEnvironment.MapPath("~/App_Data/Logs");

                // Fallback si no estamos en contexto Web (ej: Tests)
                if (string.IsNullOrEmpty(folderPath))
                {
                    folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                }

                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                string fileName = $"LogApi_{DateTime.Now:yyyyMMdd}.txt";
                string fullPath = Path.Combine(folderPath, fileName);

                // Extraer contexto HTTP completo
                var ctx    = HostingEnvironment.IsHosted ? HttpContext.Current : null;
                string ip      = ctx?.Request?.UserHostAddress ?? "Internal";
                string url     = ctx?.Request?.Url?.PathAndQuery ?? "N/A";
                string method  = ctx?.Request?.HttpMethod ?? "N/A";
                string user    = ctx?.User?.Identity?.Name;
                string agent   = ctx?.Request?.UserAgent ?? "N/A";
                string cId     = corrId ?? (ctx?.Request?.Headers["X-Correlation-ID"]) ?? "N/A";
                string rId     = recordId.HasValue ? $" [RID: {recordId}]" : "";
                string userStr = string.IsNullOrEmpty(user) ? "Anonymous" : user;

                // Cabecera común a todos los niveles
                string header = $"{DateTime.Now:HH:mm:ss} [API][{type}]" +
                                $" [CorrID: {cId}] [IP: {ip}] [Method: {method}]" +
                                $" [User: {userStr}] [URL: {url}]{rId}" +
                                $" [Agent: {Truncate(agent, 120)}]";

                string logEntry;

                bool isHighLevel = type == "WARN" || type == "ERROR" || type == "FATAL";

                if (!isHighLevel)
                {
                    // ── Línea simple para INFO y DEBUG ──────────────────────
                    logEntry = $"{header} | {message}{Environment.NewLine}";
                }
                else
                {
                    // ── Bloque estructurado con separadores para WARN/ERROR/FATAL ──
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine($"{_separator} {type} {_separator}");
                    sb.AppendLine(header);
                    sb.AppendLine($"MENSAJE  : {message}");

                    if (ex != null)
                    {
                        sb.AppendLine($"EXCEPTION: {ex.Message}");
                        if (ex.InnerException != null)
                            sb.AppendLine($"INNER EX : {ex.InnerException.Message}");
                    }

                    if (!string.IsNullOrEmpty(sqlQuery))
                        sb.AppendLine($"SQL      : {sqlQuery}");

                    if (ex?.StackTrace != null)
                    {
                        sb.AppendLine("STACKTRACE:");
                        sb.AppendLine(ex.StackTrace);
                    }

                    sb.AppendLine(_separator);
                    sb.AppendLine(); // Línea en blanco para separar visualmente del siguiente registro
                    logEntry = sb.ToString();
                }

                lock (_lock)
                {
                    File.AppendAllText(fullPath, logEntry);
                }

                CleanupOldLogs(folderPath);
            }
            catch (Exception fallbackEx)
            {
                try { System.Diagnostics.EventLog.WriteEntry("Application", "GESAP Log Error (File): " + fallbackEx.Message, System.Diagnostics.EventLogEntryType.Error); } catch { }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // ESCRITURA EN BASE DE DATOS ORACLE
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Registra un log en la tabla RECORDATORIO_ENVIO_LOGS de Oracle.
        /// Si falla, escribe el error en el fichero TXT (nunca se pierde el log).
        /// </summary>
        private static void LogToDb(string message, string level, Exception ex, string sqlQuery, string corrId = null, long? recordId = null)
        {
            try
            {
                var factory = new OracleConnectionFactory();
                using (var conn = (OracleConnection)factory.CreateConnection())
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        try
                        {
                            // 1. Obtener el siguiente ID (MAX + 1)
                            long nextId;
                            using (var cmdMax = conn.CreateCommand())
                            {
                                cmdMax.Transaction = tx;
                                cmdMax.CommandText = "SELECT NVL(MAX(LOG_ID), 0) + 1 FROM GESAP.RECORDATORIO_ENVIO_LOGS";
                                nextId = Convert.ToInt64(cmdMax.ExecuteScalar());
                            }

                            // 2. Insertar el log
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.Transaction = tx;
                                ((OracleCommand)cmd).BindByName = true;
                                cmd.CommandText = @"
                                    INSERT INTO GESAP.RECORDATORIO_ENVIO_LOGS 
                                        (LOG_ID, LOG_DATE, LOG_LEVEL, LOGGER, MESSAGE, EXCEPTION_MSG, STACKTRACE, IP_ADDRESS, ENDPOINT, USER_NAME, SQL_QUERY, HTTP_METHOD, USER_AGENT, CORRELATION_ID, RECORD_ID)
                                    VALUES 
                                        (:v_id, SYSDATE, :v_level, :v_logger, :v_msg, :v_exMsg, :v_stack, :v_ip, :v_endpoint, :v_user, :v_sql, :v_method, :v_agent, :v_corr, :v_rid)";

                                // Obtener contexto de la petición si existe
                                var ctx = HttpContext.Current;
                                string ip       = ctx?.Request?.UserHostAddress ?? "Internal";
                                string endpoint = ctx?.Request?.Url?.PathAndQuery ?? "N/A";
                                string user     = ctx?.User?.Identity?.Name ?? "Anonymous";
                                string method   = ctx?.Request?.HttpMethod ?? "N/A";
                                string agent    = ctx?.Request?.UserAgent ?? "N/A";
                                string cId      = corrId ?? (ctx?.Request?.Headers["X-Correlation-ID"]) ?? "N/A";

                                cmd.Parameters.Add(new OracleParameter("v_id",     nextId));
                                cmd.Parameters.Add(new OracleParameter("v_level",  level));
                                cmd.Parameters.Add(new OracleParameter("v_logger", "Infrastructure"));
                                cmd.Parameters.Add(new OracleParameter("v_msg",    message?.Length > 4000 ? message.Substring(0, 3997) + "..." : message));

                                var exParam = new OracleParameter("v_exMsg", OracleDbType.Clob);
                                exParam.Value = ex?.Message ?? (object)DBNull.Value;
                                cmd.Parameters.Add(exParam);

                                var stackParam = new OracleParameter("v_stack", OracleDbType.Clob);
                                stackParam.Value = ex?.StackTrace ?? (object)DBNull.Value;
                                cmd.Parameters.Add(stackParam);

                                cmd.Parameters.Add(new OracleParameter("v_ip",       ip));
                                cmd.Parameters.Add(new OracleParameter("v_endpoint", endpoint));
                                cmd.Parameters.Add(new OracleParameter("v_user",     user));

                                var sqlParam = new OracleParameter("v_sql", OracleDbType.Clob);
                                sqlParam.Value = sqlQuery ?? (object)DBNull.Value;
                                cmd.Parameters.Add(sqlParam);

                                cmd.Parameters.Add(new OracleParameter("v_method", method));

                                var agentParam = new OracleParameter("v_agent", OracleDbType.Clob);
                                agentParam.Value = agent ?? (object)DBNull.Value;
                                cmd.Parameters.Add(agentParam);

                                cmd.Parameters.Add(new OracleParameter("v_corr", cId));
                                cmd.Parameters.Add(new OracleParameter("v_rid",  recordId ?? (object)DBNull.Value));

                                cmd.ExecuteNonQuery();
                            }

                            tx.Commit();
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception dbEx)
            {
                // Si falla la BD, escribimos el error del log al fichero para no perder nada
                WriteToFile($"FALLO AL GRABAR LOG EN BD: {dbEx.Message}", "FATAL", corrId, recordId);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // LECTURA DE LOGS DESDE BD (para Lanzadera)
        // ─────────────────────────────────────────────────────────────────────

        public static System.Collections.Generic.List<object> GetDbLogs(int count = 500)
        {
            var logs = new System.Collections.Generic.List<object>();
            try
            {
                var factory = new OracleConnectionFactory();
                using (var conn = (OracleConnection)factory.CreateConnection())
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = $@"
                            SELECT * FROM (
                                SELECT LOG_ID, LOG_DATE, LOG_LEVEL, MESSAGE, IP_ADDRESS, ENDPOINT, HTTP_METHOD, CORRELATION_ID, RECORD_ID
                                FROM GESAP.RECORDATORIO_ENVIO_LOGS
                                ORDER BY LOG_DATE DESC
                            ) WHERE ROWNUM <= {count}";

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                logs.Add(new
                                {
                                    Id     = reader["LOG_ID"],
                                    Date   = reader["LOG_DATE"] == DBNull.Value ? "---" : ((DateTime)reader["LOG_DATE"]).ToString("yyyy-MM-dd HH:mm:ss"),
                                    Level  = reader["LOG_LEVEL"].ToString(),
                                    Msg    = reader["MESSAGE"].ToString(),
                                    Ip     = reader["IP_ADDRESS"].ToString(),
                                    Url    = reader["ENDPOINT"].ToString(),
                                    Method = reader["HTTP_METHOD"].ToString(),
                                    CorrId = reader["CORRELATION_ID"].ToString(),
                                    Rid    = reader["RECORD_ID"] == DBNull.Value ? "" : reader["RECORD_ID"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteToFile($"Error GetDbLogs: {ex.Message}", "ERROR");
            }
            return logs;
        }

        // ─────────────────────────────────────────────────────────────────────
        // UTILIDADES PRIVADAS
        // ─────────────────────────────────────────────────────────────────────

        private static void CleanupOldLogs(string folderPath)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-GetMaxDaysLog());
                var files = Directory.GetFiles(folderPath, "LogApi_*.txt");
                foreach (var file in files)
                {
                    var fi = new FileInfo(file);
                    if (fi.CreationTime < cutoffDate) { try { fi.Delete(); } catch { } }
                }
            }
            catch (Exception ex)
            {
                try { System.Diagnostics.EventLog.WriteEntry("Application", "GESAP Log Cleanup Error: " + ex.Message, System.Diagnostics.EventLogEntryType.Error); } catch { }
            }
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
        }
    }
}
