using System;
using System.Collections.Generic;
using System.Diagnostics;
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