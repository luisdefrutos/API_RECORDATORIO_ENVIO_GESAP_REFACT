using System;
using System.Data;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using RecordatorioEnvio.Infrastructure.Data;
using RecordatorioEnvio.Infrastructure.Logging;
using RecordatorioEnvio.Infrastructure.Repositories;

namespace RecordatorioEnvio.Tests.Integration.Logging
{
    [TestClass]
    public class LoggingIntegrationTests
    {
        private OracleConnectionFactory _factory;
        private string _testId;

        [TestInitialize]
        public void SetUp()
        {
            _factory = new OracleConnectionFactory();
            _testId = "TEST_" + Guid.NewGuid().ToString().Substring(0, 8);
        }

        //[TestCleanup]
        //public void TearDown()
        //{
        //    // Limpieza de logs de prueba
        //    try
        //    {
        //        using (var conn = _factory.CreateConnection())
        //        {
        //            conn.Open();
        //            using (var cmd = conn.CreateCommand())
        //            {
        //                cmd.CommandText = "DELETE FROM GESAP.RECORDATORIO_ENVIO_LOGS WHERE MESSAGE LIKE :testId";
        //                var p = cmd.CreateParameter();
        //                p.ParameterName = "testId";
        //                p.Value = "%" + _testId + "%";
        //                cmd.Parameters.Add(p);
        //                cmd.ExecuteNonQuery();
        //            }
        //        }
        //    }
        //    catch { /* Ignore */ }
        //}

        [TestMethod]
        public void LogHelper_Info_WritesToDatabaseWithManualId()
        {
            // Arrange
            string message = $"Mensaje de prueba integración {_testId}";

            // Act
            LogHelper.Info(message);

            // Assert
            var logCount = GetLogCount(message);
            Assert.AreEqual(1, logCount, "Debería haberse insertado exactamente un registro de log.");
        }

        [TestMethod]
        public void LogHelper_MultipleLogs_GenerateSequentialIds()
        {
            // Arrange
            string msg1 = $"Msg 1 Sequential {_testId}";
            string msg2 = $"Msg 2 Sequential {_testId}";

            // Act
            LogHelper.Info(msg1);
            LogHelper.Info(msg2);

            // Assert
            using (var conn = _factory.CreateConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT LOG_ID FROM GESAP.RECORDATORIO_ENVIO_LOGS WHERE MESSAGE LIKE :testId ORDER BY LOG_ID";
                    var p = cmd.CreateParameter();
                    p.ParameterName = "testId";
                    p.Value = "%" + _testId + "%";
                    cmd.Parameters.Add(p);

                    using (var reader = cmd.ExecuteReader())
                    {
                        Assert.IsTrue(reader.Read());
                        long id1 = Convert.ToInt64(reader["LOG_ID"]);

                        Assert.IsTrue(reader.Read());
                        long id2 = Convert.ToInt64(reader["LOG_ID"]);

                        Assert.AreEqual(id1 + 1, id2, "Los IDs generados con MAX+1 deberían ser secuenciales.");
                    }
                }
            }
        }

        private int GetLogCount(string message)
        {
            using (var conn = _factory.CreateConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM GESAP.RECORDATORIO_ENVIO_LOGS WHERE MESSAGE = :msg";
                    var p = cmd.CreateParameter();
                    p.ParameterName = "msg";
                    p.Value = message;
                    cmd.Parameters.Add(p);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }
    }
}
