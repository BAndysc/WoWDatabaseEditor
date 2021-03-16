using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common;
using WDE.Module.Attributes;
using WDE.TrinityMySqlDatabase.Providers;

namespace WDE.TrinityMySqlDatabase.ViewModels
{
    [AutoRegister]
    public class DatabaseConfigViewModel : BindableBase, IConfigurable
    {
        private readonly IConnectionSettingsProvider settings;
        private string? database;

        private string? host;
        private string? pass;
        private string? port;
        private string? user;

        public DatabaseConfigViewModel(IConnectionSettingsProvider settings)
        {
            database = settings.GetSettings().Database;
            port = (settings.GetSettings().Port ?? 3306).ToString();
            user = settings.GetSettings().User;
            pass = settings.GetSettings().Password;
            host = settings.GetSettings().Host;
            this.settings = settings;

            Save = new DelegateCommand(() =>
            {
                int? port = null;
                if (int.TryParse(Port, out int port_))
                    port = port_;
                settings.UpdateSettings(User, Password, Host, port, Database);
                IsModified = false;
            });
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

        public ICommand Save { get; }

        public string Name => "TrinityCore Database";
        public string ShortDescription => "To get all editor features, you have to connect to any Trinity compatible database.";

        private bool isModified;
        public bool IsModified
        {
            get => isModified;
            private set => SetProperty(ref isModified, value);
        }

        public bool IsRestartRequired => true;
    }
}