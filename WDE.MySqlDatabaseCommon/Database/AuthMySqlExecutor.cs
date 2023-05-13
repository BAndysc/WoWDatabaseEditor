using System;
using System.Diagnostics;
using System.Threading.Tasks;
using MySqlConnector;
using WDE.Common.Database;
using WDE.MySqlDatabaseCommon.Providers;
using WDE.MySqlDatabaseCommon.Services;

namespace WDE.MySqlDatabaseCommon.Database
{
    public class AuthMySqlExecutor : IAuthMySqlExecutor
    {
        private readonly IMySqlAuthConnectionStringProvider authConnectionString;
        private readonly IAuthDatabaseProvider databaseProvider;
        private readonly DatabaseLogger databaseLogger;

        public AuthMySqlExecutor(IMySqlAuthConnectionStringProvider authConnectionString,
            IAuthDatabaseProvider databaseProvider,
            DatabaseLogger databaseLogger)
        {
            this.authConnectionString = authConnectionString;
            this.databaseProvider = databaseProvider;
            this.databaseLogger = databaseLogger;
        }

        public bool IsConnected => databaseProvider.IsConnected;
        
        public async Task ExecuteSql(string query)
        {
            if (string.IsNullOrEmpty(query) || !IsConnected)
                return;

            databaseLogger.Log(query, null, TraceLevel.Info, QueryType.WriteQuery);
            
            using var writeLock = await DatabaseLock.WriteLock();
            
            MySqlConnection conn = new(authConnectionString.ConnectionString);
            MySqlTransaction transaction;
            try
            {
                await conn.OpenAsync();
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
            catch (MySqlConnector.MySqlException e)
            {
                await transaction.RollbackAsync();
                await conn.CloseAsync();
                throw new IMySqlExecutor.QueryFailedDatabaseException(e.Message, e);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                await conn.CloseAsync();
                throw new IMySqlExecutor.QueryFailedDatabaseException(ex);
            }
            await conn.CloseAsync();
        }
    }
}