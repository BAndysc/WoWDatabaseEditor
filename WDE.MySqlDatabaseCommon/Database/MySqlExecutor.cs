using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MySqlConnector;
using WDE.Common.Database;
using WDE.MySqlDatabaseCommon.Providers;
using WDE.MySqlDatabaseCommon.Services;

namespace WDE.MySqlDatabaseCommon.Database
{
    public class MySqlExecutor : IMySqlExecutor
    {
        private readonly IMySqlConnectionStringProvider connectionString;
        private readonly IDatabaseProvider databaseProvider;
        private readonly DatabaseLogger databaseLogger;
        
        public bool IsConnected => databaseProvider.IsConnected;

        public MySqlExecutor(IMySqlConnectionStringProvider connectionString,
            IDatabaseProvider databaseProvider,
            DatabaseLogger databaseLogger)
        {
            this.connectionString = connectionString;
            this.databaseProvider = databaseProvider;
            this.databaseLogger = databaseLogger;
        }

        public async Task<IList<string>> GetTables()
        {
            var tables = await ExecuteSelectSql("SHOW TABLES");

            return tables.SelectMany(row => row.Values).Select(c => (string)c.Item2).ToList();
        }

        public async Task<IList<MySqlDatabaseColumn>> GetTableColumns(string table)
        {
            string databaseName = connectionString.DatabaseName;
            var columns = await ExecuteSelectSql($"SELECT COLUMN_NAME, COLUMN_DEFAULT, IS_NULLABLE, DATA_TYPE, COLUMN_TYPE, COLUMN_KEY FROM information_schema.columns WHERE table_name = '{table}' AND table_schema = '{databaseName}' ORDER BY ordinal_position;");

            var list = new List<MySqlDatabaseColumn>();
            foreach (var column in columns)
            {
                var c = new MySqlDatabaseColumn()
                {
                    ColumnName = (string)column["COLUMN_NAME"].Item2
                };

                c.Nullable = (string) column["IS_NULLABLE"].Item2 == "YES";
                c.PrimaryKey = (string) column["COLUMN_KEY"].Item2 == "PRI";

                bool unsigned = ((string) column["COLUMN_TYPE"].Item2).Contains("unsigned");
                string dataType = (string) column["DATA_TYPE"].Item2;
                string? defaultValue = null;
                if (column["COLUMN_DEFAULT"].Item2 is string str)
                    defaultValue = str;

                c.DatabaseType = dataType;
                c.ManagedType = null;
                switch (dataType)
                {
                    case "float":
                        c.ManagedType = typeof(float);
                        if (defaultValue != null && float.TryParse(defaultValue, out var defaultFloat))
                            c.DefaultValue = defaultFloat;
                        break;
                    case "int":
                        c.ManagedType = unsigned ? typeof(ulong) : typeof(long);
                        if (defaultValue != null)
                            if (c.ManagedType == typeof(ulong))
                            {
                                if (ulong.TryParse(defaultValue, out var defaultInt))
                                    c.DefaultValue = defaultInt;
                            }
                            else
                            {
                                if (long.TryParse(defaultValue, out var defaultInt))
                                    c.DefaultValue = defaultInt;
                            }
                        break;
                    case "tinyint":
                        c.ManagedType = unsigned ? typeof(byte) : typeof(sbyte);
                        if (defaultValue != null)
                            if (c.ManagedType == typeof(byte))
                            {
                                if (byte.TryParse(defaultValue, out var defaultV))
                                    c.DefaultValue = defaultV;
                            }
                            else
                            {
                                if (sbyte.TryParse(defaultValue, out var defaultV))
                                    c.DefaultValue = defaultV;
                            }
                        break;
                    case "smallint":
                        c.ManagedType = unsigned ? typeof(ushort) : typeof(short);
                        if (defaultValue != null)
                            if (c.ManagedType == typeof(ushort))
                            {
                                if (ushort.TryParse(defaultValue, out var defaultV))
                                    c.DefaultValue = defaultV;
                            }
                            else
                            {
                                if (short.TryParse(defaultValue, out var defaultV))
                                    c.DefaultValue = defaultV;
                            }
                        break;
                    case "mediumint":
                        c.ManagedType = unsigned ? typeof(uint) : typeof(int);
                        if (defaultValue != null)
                            if (c.ManagedType == typeof(uint))
                            {
                                if (uint.TryParse(defaultValue, out var defaultV))
                                    c.DefaultValue = defaultV;
                            }
                            else
                            {
                                if (int.TryParse(defaultValue, out var defaultV))
                                    c.DefaultValue = defaultV;
                            }
                        break;
                    case "char":
                    case "varchar":
                    case "text":
                    case "longtext":
                        c.ManagedType = typeof(string);
                        c.DefaultValue = defaultValue;
                        break;
                }
                list.Add(c);
            }
            return list;
        }
        
        public async Task ExecuteSql(string query)
        {
            if (string.IsNullOrEmpty(query) || !IsConnected)
                return;

            databaseLogger.Log(query, null, TraceLevel.Info);
            
            using var writeLock = await DatabaseLock.WriteLock();
            
            MySqlConnection conn = new(connectionString.ConnectionString);
            MySqlTransaction transaction;
            try
            {
                conn.Open();
                transaction = await conn.BeginTransactionAsync();
            }
            catch (Exception e)
            {
                throw new IMySqlExecutor.CannotConnectToDatabaseException(e);
            }

            try
            {
                MySqlCommand cmd = new(query, conn, transaction);
                await cmd.ExecuteNonQueryAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                await conn.CloseAsync();
                throw new IMySqlExecutor.QueryFailedDatabaseException(ex);
            }
            await conn.CloseAsync();
        }

        public async Task<IList<Dictionary<string, (Type, object)>>> ExecuteSelectSql(string query)
        {
            if (string.IsNullOrEmpty(query) || !IsConnected)
                return new List<Dictionary<string, (Type, object)>>();

            databaseLogger.Log(query, null, TraceLevel.Info);
            using var writeLock = await DatabaseLock.WriteLock();
            
            MySqlConnection conn = new(connectionString.ConnectionString);
            try
            {
                conn.Open();
            }
            catch (Exception e)
            {
                throw new IMySqlExecutor.CannotConnectToDatabaseException(e);
            }

            MySqlDataReader reader;
            try
            {
                MySqlCommand cmd = new(query, conn);
                reader = await cmd.ExecuteReaderAsync();
            }
            catch (Exception ex)
            {
                await conn.CloseAsync();
                throw new IMySqlExecutor.QueryFailedDatabaseException(ex);
            }

            List<Dictionary<string, (Type, object)>> result = new();
            while (reader.Read())
            {
                var fields = new Dictionary<string, (Type, object)>(reader.FieldCount);
                for (int i = 0; i < reader.FieldCount; ++i)
                    fields.Add(reader.GetName(i), (reader.GetFieldType(i), reader.GetValue(i)));
                
                result.Add(fields);
            }

            await reader.CloseAsync();
            await conn.CloseAsync();
            return result;
        }
    }
}