using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Microsoft.Practices.Unity;
using Newtonsoft.Json;
using Prism.Modularity;
using WDE.Common;
using WDE.Common.Database;
using WDE.MySqlDatabase.Data;
using WDE.MySqlDatabase.ViewModels;
using WDE.MySqlDatabase.Views;

namespace WDE.MySqlDatabase
{
    public class MySqlDatabaseModule : IModule, IConfigurable
    {
        private IUnityContainer container;

        public static DbAccess DbAccess { get; set; }

        public MySqlDatabaseModule(IUnityContainer container)
        {
            this.container = container;
        }

        public void Initialize()
        {
            if (System.IO.File.Exists("database.json"))
            {
                JsonSerializer ser = new Newtonsoft.Json.JsonSerializer() { TypeNameHandling = TypeNameHandling.Auto };
                using (StreamReader re = new StreamReader("database.json"))
                {
                    JsonTextReader reader = new JsonTextReader(re);
                    DbAccess = ser.Deserialize<DbAccess>(reader);
                }
            } else
                DbAccess = new DbAccess();
            container.RegisterType<IDatabaseProvider, MysqlDatabaseProvider>(new ContainerControlledLifetimeManager());
            container.RegisterType<IConfigurable, MySqlDatabaseModule>("Database configuration");
        }

        public KeyValuePair<ContentControl, Action> GetConfigurationView()
        {
            var view = new DatabaseConfigView();
            var viewModel = new DatabaseConfigViewModel();
            view.DataContext = viewModel;
            return new KeyValuePair<ContentControl, Action>(view, viewModel.SaveAction);
        }

        public string GetName()
        {
            return "Database";
        }
    }
}