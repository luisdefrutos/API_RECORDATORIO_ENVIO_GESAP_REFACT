using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace RecordatorioEnvio.Infrastructure.Data
{
    public class OracleConnectionFactory
    {
        private readonly string _connectionString;

        public OracleConnectionFactory()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["OracleConnection"].ConnectionString;
        }

        public IDbConnection CreateConnection()
        {
            return new OracleConnection(_connectionString);
        }
    }
}
