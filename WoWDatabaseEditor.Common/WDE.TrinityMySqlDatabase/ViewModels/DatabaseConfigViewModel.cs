using System;
using Prism.Mvvm;
using WDE.TrinityMySqlDatabase.Providers;

namespace WDE.TrinityMySqlDatabase.ViewModels
{
    public class DatabaseConfigViewModel : BindableBase
    {
        private readonly IConnectionSettingsProvider settings;
        private string? database;

        private string? host;
        private string? pass;
        private string? port;
        private string? user;

        public DatabaseConfigViewModel(IConnectionSettingsProvider settings)
        {
            SaveAction = Save;
            Database = settings.GetSettings().Database;
            Port = (settings.GetSettings().Port ?? 3306).ToString();
            User = settings.GetSettings().User;
            Password = settings.GetSettings().Password;
            Host = settings.GetSettings().Host;
            this.settings = settings;
        }

        public Action SaveAction { get; set; }

        public string? Host
        {
            get => host;
            set => SetProperty(ref host, value);
        }

        public string? Port
        {
            get => port;
            set => SetProperty(ref port, value);
        }

        public string? User
        {
            get => user;
            set => SetProperty(ref user, value);
        }

        public string? Password
        {
            get => pass;
            set => SetProperty(ref pass, value);
        }

        public string? Database
        {
            get => database;
            set => SetProperty(ref database, value);
        }

        private void Save()
        {
            int? port = null;
            if (int.TryParse(Port, out int port_))
                port = port_;
            settings.UpdateSettings(User, Password, Host, port, Database);
        }
    }
}