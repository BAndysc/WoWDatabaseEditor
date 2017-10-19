using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Windows;
using Newtonsoft.Json;

namespace WDE.MySqlDatabase.ViewModels
{
    public class DatabaseConfigViewModel : BindableBase
    {
        public Action SaveAction { get; set; }

        private string _host;
        private string _user;
        private string _pass;
        private string _database;

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


        public DatabaseConfigViewModel()
        {
            SaveAction = Save;
            Database = MySqlDatabaseModule.DbAccess.DB;
            User = MySqlDatabaseModule.DbAccess.User;
            Password = MySqlDatabaseModule.DbAccess.Password;
            Host = MySqlDatabaseModule.DbAccess.Host;
        }

        private void Save()
        {
            MySqlDatabaseModule.DbAccess.DB = Database;
            MySqlDatabaseModule.DbAccess.User = User;
            MySqlDatabaseModule.DbAccess.Password = Password;
            MySqlDatabaseModule.DbAccess.Host = Host;
            JsonSerializer ser = new Newtonsoft.Json.JsonSerializer() { TypeNameHandling = TypeNameHandling.Auto };
            using (StreamWriter file = File.CreateText(@"database.json"))
            {
                ser.Serialize(file, MySqlDatabaseModule.DbAccess);
            }
            MessageBox.Show("Restart the application.");
        }
    }
}
