using System;
using System.Diagnostics;
using System.Threading.Tasks;
using MySqlConnector;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.MySqlDatabaseCommon.Providers;
using WDE.MySqlDatabaseCommon.Services;

namespace WDE.MySqlDatabaseCommon.Database
{
    public class AuthMySqlExecutor : IAuthMySqlExecutor
    {
        private readonly IMySqlAuthConnectionStringProvider authConnectionString;
        private readonly IAuthDatabaseProvider databaseProvider;
        private readonly ICurrentCoreVersion currentCoreVersion;
        private readonly DatabaseLogger databaseLogger;

        public AuthMySqlExecutor(IMySqlAuthConnectionStringProvider authConnectionString,
            IAuthDatabaseProvider databaseProvider,
            ICurrentCoreVersion currentCoreVersion,
            DatabaseLogger databaseLogger)
        {
            this.authConnectionString = authConnectionString;
            this.databaseProvider = databaseProvider;
            this.currentCoreVersion = currentCoreVersion;
            this.databaseLogger = databaseLogger;
        }

        public bool IsConnected => databaseProvider.IsConnected;
        
        public async Task ExecuteSql(string query)
        {
            if (string.IsNullOrEmpty(query) || !IsConnected)
                return;

            databaseLogger.Log(query, null, TraceLevel.Info, QueryType.WriteQuery);
            
            using var writeLock = await DatabaseLock.WriteLock();

            // when the core mixes InnoDB and MyISAM tables (cmangos), a transaction cannot
            // span both engines, so run without one
            bool useTransaction = currentCoreVersion.Current.DatabaseFeatures.SupportsTransactions;

            MySqlConnection conn = new(authConnectionString.ConnectionString);
            MySqlTransaction? transaction = null;
            try
            {
                await conn.OpenAsync();
                if (useTransaction)
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
                if (transaction != null)
                    await transaction.CommitAsync();
            }
            catch (MySqlConnector.MySqlException e)
            {
                if (transaction != null)
                    await transaction.RollbackAsync();
                await conn.CloseAsync();
                throw new IMySqlExecutor.QueryFailedDatabaseException(e.Message, e);
            }
            catch (Exception ex)
            {
                if (transaction != null)
                    await transaction.RollbackAsync();
                await conn.CloseAsync();
                throw new IMySqlExecutor.QueryFailedDatabaseException(ex);
            }
            await conn.CloseAsync();
        }
    }
}