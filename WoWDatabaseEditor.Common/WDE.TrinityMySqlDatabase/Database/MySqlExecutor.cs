using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySqlConnector;
using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.TrinityMySqlDatabase.Providers;

namespace WDE.TrinityMySqlDatabase.Database
{
    [AutoRegister]
    [SingleInstance]
    public class MySqlExecutor : IMySqlExecutor
    {
        private readonly MySqlSettings settings;

        public MySqlExecutor(IConnectionSettingsProvider connectionSettingsProvider)
        {
            settings = new MySqlSettings(connectionSettingsProvider.GetSettings());
        }

        public async Task ExecuteSql(string query)
        {
            using var writeLock = await MySqlSingleWriteLock.WriteLock();
            
            string connStr = settings.ConnectionStrings.First().ConnectionString;
            MySqlConnection conn = new(connStr);
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

        public async Task<IList<Dictionary<string, object>>> ExecuteSelectSql(string query)
        {
            using var writeLock = await MySqlSingleWriteLock.WriteLock();
            
            string connStr = settings.ConnectionStrings.First().ConnectionString;
            MySqlConnection conn = new(connStr);
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

            List<Dictionary<string, object>> result = new();
            while (reader.Read())
            {
                var fields = new Dictionary<string, object>(reader.FieldCount);
                for (int i = 0; i < reader.FieldCount; ++i)
                    fields.Add(reader.GetName(i), reader.GetValue(i));
                
                result.Add(fields);
            }

            await reader.CloseAsync();
            await conn.CloseAsync();
            return result;
        }
    }
}