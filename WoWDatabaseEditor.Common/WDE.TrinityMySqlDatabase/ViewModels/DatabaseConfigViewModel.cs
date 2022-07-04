using WDE.Module.Attributes;
using WDE.MySqlDatabaseCommon.Providers;
using WDE.MySqlDatabaseCommon.ViewModels;

namespace WDE.TrinityMySqlDatabase.ViewModels
{
    [AutoRegister]
    public class DatabaseConfigViewModel : BaseDatabaseConfigViewModel
    {
        public DatabaseConfigViewModel(IWorldDatabaseSettingsProvider worldSettingsProvider,
            IAuthDatabaseSettingsProvider authDatabaseSettingsProvider,
            IHotfixDatabaseSettingsProvider hotfixDatabaseSettingsProvider) : base(worldSettingsProvider, authDatabaseSettingsProvider, hotfixDatabaseSettingsProvider)
        {
        }

        public override string Name => "TrinityCore Database";
        public override string ShortDescription => "To get all editor features, you have to connect to any Trinity compatible database.";
    }
}