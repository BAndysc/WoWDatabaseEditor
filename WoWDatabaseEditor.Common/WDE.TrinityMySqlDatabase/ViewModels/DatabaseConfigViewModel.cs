using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common;
using WDE.Module.Attributes;
using WDE.TrinityMySqlDatabase.Data;
using WDE.TrinityMySqlDatabase.Providers;

namespace WDE.TrinityMySqlDatabase.ViewModels
{
    [AutoRegister]
    public class DatabaseConfigViewModel : BindableBase, IConfigurable
    {
        private string? database;

        private string? host;
        private string? pass;
        private string? port;
        private string? user;

        public DatabaseConfigViewModel(IDatabaseSettingsProvider settingsProvider)
        {
            var dbSettings = settingsProvider.Settings;
            database = dbSettings.Database;
            port = (dbSettings.Port ?? 3306).ToString();
            user = dbSettings.User;
            pass = dbSettings.Password;
            host = dbSettings.Host;

            Save = new DelegateCommand(() =>
            {
                int? port = null;
                if (int.TryParse(Port, out int port_))
                    port = port_;
                settingsProvider.Settings = new DbAccess()
                {
                    Host = host,
                    Database = database,
                    Password = pass,
                    Port = port,
                    User = user
                };
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