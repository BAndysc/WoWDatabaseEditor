using WDE.Module.Attributes;
using WDE.MySqlDatabaseCommon.Database.Auth;
using WDE.MySqlDatabaseCommon.Providers;
using WDE.SkyFireMySqlDatabase.Database;

namespace WDE.SkyFireMySqlDatabase
{
    [AutoRegister]
    [SingleInstance]
    public class AuthDatabaseProvider : AuthDatabaseDecorator
    {
        public AuthDatabaseProvider(SkyFireMySqlDatabaseProvider skyfireDatabase,
            NullAuthDatabaseProvider nullAuthDatabaseProvider,
            IAuthDatabaseSettingsProvider settingsProvider
        ) : base(nullAuthDatabaseProvider)
        {
            if (settingsProvider.Settings.IsEmpty)
                return;

            impl = skyfireDatabase;
        }
    }
}