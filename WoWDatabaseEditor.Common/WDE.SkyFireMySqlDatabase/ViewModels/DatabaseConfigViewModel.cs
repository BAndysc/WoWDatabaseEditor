using WDE.Module.Attributes;
using WDE.MySqlDatabaseCommon.Providers;
using WDE.MySqlDatabaseCommon.ViewModels;

namespace WDE.SkyFireMySqlDatabase.ViewModels
{
    [AutoRegister]
    public class DatabaseConfigViewModel : BaseDatabaseConfigViewModel
    {
        public DatabaseConfigViewModel(IWorldDatabaseSettingsProvider worldSettingsProvider,
            IAuthDatabaseSettingsProvider authDatabaseSettingsProvider) : base(worldSettingsProvider, authDatabaseSettingsProvider)
        {
        }

        public override string Name => "SkyFireCore Database";
        public override string ShortDescription => "To get all editor features, you have to connect to any SkyFire compatible database.";
    }
}