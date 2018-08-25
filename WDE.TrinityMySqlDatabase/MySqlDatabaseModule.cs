using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

using Newtonsoft.Json;
using Prism.Modularity;
using WDE.Common;
using WDE.Common.Database;
using WDE.TrinityMySqlDatabase.Data;
using WDE.TrinityMySqlDatabase.ViewModels;
using WDE.TrinityMySqlDatabase.Views;
using Prism.Ioc;
using WDE.Common.Attributes;

namespace WDE.TrinityMySqlDatabase
{
    [AutoRegister, SingleInstance]
    public class TrinityMySqlDatabaseModule : IModule, IConfigurable
    {
        public static DbAccess DbAccess { get; set; }
        
        public KeyValuePair<ContentControl, Action> GetConfigurationView()
        {
            var view = new DatabaseConfigView();
            var viewModel = new DatabaseConfigViewModel();
            view.DataContext = viewModel;
            return new KeyValuePair<ContentControl, Action>(view, viewModel.SaveAction);
        }

        public string GetName()
        {
            return "TrinityCore Database";
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            if (System.IO.File.Exists("database.json"))
            {
                JsonSerializer ser = new Newtonsoft.Json.JsonSerializer() { TypeNameHandling = TypeNameHandling.Auto };
                using (StreamReader re = new StreamReader("database.json"))
                {
                    JsonTextReader reader = new JsonTextReader(re);
                    DbAccess = ser.Deserialize<DbAccess>(reader);
                }
            }
            else
                DbAccess = new DbAccess();
        }
    }
}