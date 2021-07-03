using WDE.Module.Attributes;
using WDE.MySqlDatabaseCommon.Database.Auth;
using WDE.MySqlDatabaseCommon.Providers;
using WDE.TrinityMySqlDatabase.Database;

namespace WDE.TrinityMySqlDatabase
{
    [AutoRegister]
    [SingleInstance]
    public class AuthDatabaseProvider : AuthDatabaseDecorator
    {
        public AuthDatabaseProvider(TrinityMySqlDatabaseProvider trinityDatabase,
            NullAuthDatabaseProvider nullAuthDatabaseProvider,
            IAuthDatabaseSettingsProvider settingsProvider
        ) : base(nullAuthDatabaseProvider)
        {
            if (settingsProvider.Settings.IsEmpty)
                return;

            impl = trinityDatabase;
        }
    }
}