using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Text.Json;
using Oracle.ManagedDataAccess.Client;

namespace OracleSchemaReader
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["OracleDbContext"]?.ConnectionString;

                if (string.IsNullOrWhiteSpace(connectionString) || connectionString.Contains("YOUR_PASSWORD_HERE"))
                {
                    Console.WriteLine(JsonSerializer.Serialize(new { error = "Configuracion de conexion a Oracle no encontrada o esta en formato de plantilla." }));
                    return;
                }

                using (var connection = new OracleConnection(connectionString))
                {
                    connection.Open();

                    if (args.Length == 0)
                    {
                        // Modo 1: Devolver lista de tablas (filtrando tablas del sistema)
                        string sql = "SELECT TABLE_NAME FROM USER_TABLES ORDER BY TABLE_NAME";
                        var tables = new List<string>();

                        using (var cmd = new OracleCommand(sql, connection))
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                tables.Add(reader.GetString(0));
                            }
                        }

                        Console.WriteLine(JsonSerializer.Serialize(new { type = "table_list", tables = tables }));
                    }
                    else
                    {
                        // Modo 2: Devolver esquema de una tabla
                        string tableName = args[0].ToUpperInvariant();
                        var columns = new List<object>();

                        string sql = @"
                            SELECT c.COLUMN_NAME, c.DATA_TYPE, c.DATA_LENGTH, c.DATA_PRECISION, c.DATA_SCALE, c.NULLABLE,
                                   CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS IS_PRIMARY_KEY
                            FROM USER_TAB_COLUMNS c
                            LEFT JOIN (
                                SELECT cols.COLUMN_NAME
                                FROM USER_CONSTRAINTS cons, USER_CONS_COLUMNS cols
                                WHERE cols.TABLE_NAME = :tableName
                                AND cons.CONSTRAINT_TYPE = 'P'
                                AND cons.CONSTRAINT_NAME = cols.CONSTRAINT_NAME
                                AND cons.OWNER = cols.OWNER
                            ) pk ON c.COLUMN_NAME = pk.COLUMN_NAME
                            WHERE c.TABLE_NAME = :tableName
                            ORDER BY c.COLUMN_ID";

                        using (var cmd = new OracleCommand(sql, connection))
                        {
                            cmd.Parameters.Add(new OracleParameter("tableName", tableName));

                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    columns.Add(new
                                    {
                                        ColumnName = reader.GetString(0),
                                        DataType = reader.GetString(1),
                                        DataLength = reader.IsDBNull(2) ? (int?)null : Convert.ToInt32(reader.GetValue(2)),
                                        DataPrecision = reader.IsDBNull(3) ? (int?)null : Convert.ToInt32(reader.GetValue(3)),
                                        DataScale = reader.IsDBNull(4) ? (int?)null : Convert.ToInt32(reader.GetValue(4)),
                                        IsNullable = reader.GetString(5) == "Y",
                                        IsPrimaryKey = Convert.ToInt32(reader.GetValue(6)) == 1
                                    });
                                }
                            }
                        }

                        if (columns.Count == 0)
                        {
                            Console.WriteLine(JsonSerializer.Serialize(new { error = $"La tabla '{tableName}' no existe o no se tienen permisos." }));
                        }
                        else
                        {
                            Console.WriteLine(JsonSerializer.Serialize(new { type = "table_schema", table = tableName, columns = columns }));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(JsonSerializer.Serialize(new { error = ex.Message, stackTrace = ex.StackTrace }));
            }
        }
    }
}
