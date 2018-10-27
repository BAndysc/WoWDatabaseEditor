using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Windows;
using Newtonsoft.Json;
using WDE.TrinityMySqlDatabase.Providers;

namespace WDE.TrinityMySqlDatabase.ViewModels
{
    public class DatabaseConfigViewModel : BindableBase
    {
        public Action SaveAction { get; set; }

        private string _host;
        private string _user;
        private string _pass;
        private string _database;
        private readonly IConnectionSettingsProvider settings;

        public string Host
        {
            get { return _host; }
            set { SetProperty(ref _host, value); }
        }

        public string User
        {
            get { return _user; }
            set { SetProperty(ref _user, value); }
        }

        public string Password
        {
            get { return _pass; }
            set { SetProperty(ref _pass, value); }
        }

        public string Database
        {
            get { return _database; }
            set { SetProperty(ref _database, value); }
        }


        public DatabaseConfigViewModel(IConnectionSettingsProvider settings)
        {
            SaveAction = Save;
            Database = settings.GetSettings().DB;
            User = settings.GetSettings().User;
            Password = settings.GetSettings().Password;
            Host = settings.GetSettings().Host;
            this.settings = settings;
        }

        private void Save()
        {
            settings.UpdateSettings(User, Password, Host, Database);
        }
    }
}
