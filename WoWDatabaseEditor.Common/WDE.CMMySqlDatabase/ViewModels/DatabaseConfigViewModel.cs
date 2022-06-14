using WDE.Module.Attributes;
using WDE.MySqlDatabaseCommon.Providers;
using WDE.MySqlDatabaseCommon.ViewModels;

namespace WDE.CMMySqlDatabase.ViewModels
{
    [AutoRegister]
    public class CMDatabaseConfigViewModel : BaseDatabaseConfigViewModel
    {
        public CMDatabaseConfigViewModel(IWorldDatabaseSettingsProvider worldSettingsProvider,
            IAuthDatabaseSettingsProvider authDatabaseSettingsProvider) : base(worldSettingsProvider, authDatabaseSettingsProvider)
        {
        }

        public override string Name => "CMaNGOS Database";
        public override string ShortDescription => "To get all editor features, you have to connect to any CMaNGOS compatible database.";
    }
}