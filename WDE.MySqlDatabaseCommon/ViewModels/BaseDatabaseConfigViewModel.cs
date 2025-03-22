using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common;
using WDE.Common.Types;
using WDE.MVVM;
using WDE.MySqlDatabaseCommon.Providers;

namespace WDE.MySqlDatabaseCommon.ViewModels
{
    public abstract class BaseDatabaseConfigViewModel : ObservableBase, IFirstTimeWizardConfigurable
    {
        public BaseDatabaseConfigViewModel(IWorldDatabaseSettingsProvider worldSettingsProvider,
            IAuthDatabaseSettingsProvider authDatabaseSettingsProvider,
            IHotfixDatabaseSettingsProvider hotfixDatabaseSettingsProvider)
        {
            var dbSettings = worldSettingsProvider.Settings;
            WorldDatabase.Database = dbSettings.Database;
            WorldDatabase.Port = (dbSettings.Port ?? 3306).ToString();
            WorldDatabase.User = dbSettings.User;
            WorldDatabase.Password = dbSettings.Password;
            WorldDatabase.Host = dbSettings.Host;
            WorldDatabase.IsModified = false;
            
            var authDbSettings = authDatabaseSettingsProvider.Settings;
            AuthDatabase.Database = authDbSettings.Database;
            AuthDatabase.Port = (authDbSettings.Port ?? 3306).ToString();
            AuthDatabase.User = authDbSettings.User;
            AuthDatabase.Password = authDbSettings.Password;
            AuthDatabase.Host = authDbSettings.Host;
            AuthDatabase.IsModified = false;
            
            var hotfixDbSettings = hotfixDatabaseSettingsProvider.Settings;
            HotfixDatabase.Database = hotfixDbSettings.Database;
            HotfixDatabase.Port = (hotfixDbSettings.Port ?? 3306).ToString();
            HotfixDatabase.User = hotfixDbSettings.User;
            HotfixDatabase.Password = hotfixDbSettings.Password;
            HotfixDatabase.Host = hotfixDbSettings.Host;
            HotfixDatabase.IsModified = false;
            
            
            Save = new DelegateCommand(() =>
            {
                if (AuthDatabase.IsModified)
                {
                    authDatabaseSettingsProvider.Settings = AuthDatabase;
                    AuthDatabase.IsModified = false;
                }

                if (WorldDatabase.IsModified)
                {
                    worldSettingsProvider.Settings = WorldDatabase;
                    WorldDatabase.IsModified = false;
                }
                
                if (HotfixDatabase.IsModified)
                {
                    hotfixDatabaseSettingsProvider.Settings = HotfixDatabase;
                    HotfixDatabase.IsModified = false;
                }
            });
            
            Watch(WorldDatabase, s => s.IsModified, nameof(IsModified));
            Watch(AuthDatabase, s => s.IsModified, nameof(IsModified));
            Watch(HotfixDatabase, s => s.IsModified, nameof(IsModified));
        }

        public ICommand Save { get; }
        public abstract string Name { get; }
        public ImageUri Icon { get; } = new ImageUri("Icons/document_sql0_big.png");
        public abstract string? ShortDescription { get; }
        public SettingsViewModel WorldDatabase { get; } = new SettingsViewModel();
        public SettingsViewModel AuthDatabase { get; } = new SettingsViewModel();
        public SettingsViewModel HotfixDatabase { get; } = new SettingsViewModel();
        public bool IsModified => WorldDatabase.IsModified || AuthDatabase.IsModified || HotfixDatabase.IsModified;
        public bool IsRestartRequired => true;
        public ConfigurableGroup Group => ConfigurableGroup.Basic;
    }
    
    public class SettingsViewModel : BindableBase, IDbAccess
    {
        private string? database;
        private string? host;
        private string? pass;
        private string? port;
        private string? user;
        
        private bool isModified;

        public bool IsModified
        {
            get => isModified;
            set => SetProperty(ref isModified, value);
        }
        
        public string? Host
        {
            get => host;
            set
            {
                IsModified = true;
                SetProperty(ref host, value);
            }
        }

        public string? Port
        {
            get => port;
            set
            {
                IsModified = true;
                SetProperty(ref port, value);
            }
        }

        int? IDbAccess.Port
        {
            get
            {
                if (int.TryParse(Port, out var _port))
                    return _port;
                return 0;
            }
            set => Port = value.ToString();
        }

        public string? User
        {
            get => user;
            set
            {
                IsModified = true;
                SetProperty(ref user, value);
            }
        }

        public string? Password
        {
            get => pass;
            set
            {
                IsModified = true;
                SetProperty(ref pass, value);
            }
        }

        public string? Database
        {
            get => database;
            set
            {
                IsModified = true;
                SetProperty(ref database, value);
            }
        }

        public bool IsEmpty => false;
    }
}