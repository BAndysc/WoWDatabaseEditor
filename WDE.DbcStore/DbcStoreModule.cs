using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

using Prism.Modularity;
using WDE.Common;
using WDE.Common.DBC;
using Prism.Ioc;
using WDE.Common.Parameters;
using WDE.Common.Attributes;
using WDE.DbcStore.Data;
using Newtonsoft.Json;
using System.IO;
using WDE.DbcStore.Views;
using WDE.DbcStore.ViewModels;

namespace WDE.DbcStore
{
    [AutoRegister]
    public class DbcStoreModule : IModule, IConfigurable
    {
        internal static DBCSettings DBCSettings;

        private readonly IParameterFactory parameterFactory;
        private DbcStore store;

        public DbcStoreModule(IParameterFactory parameterFactory)
        {
            this.parameterFactory = parameterFactory;
        }

        public KeyValuePair<ContentControl, Action> GetConfigurationView()
        {
            var view = new DBCConfigView();
            var viewModel = new DBCConfigViewModel();
            view.DataContext = viewModel;
            return new KeyValuePair<ContentControl, Action>(view, viewModel.SaveAction);
        }

        public string GetName()
        {
            return "DBC";
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            if (File.Exists("dbc.json"))
            {
                JsonSerializer ser = new Newtonsoft.Json.JsonSerializer() { TypeNameHandling = TypeNameHandling.Auto };
                using (StreamReader re = new StreamReader("dbc.json"))
                {
                    JsonTextReader reader = new JsonTextReader(re);
                    DBCSettings = ser.Deserialize<DBCSettings>(reader);
                }
            }
            else
                DBCSettings = new DBCSettings();

            store.Load();
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            store = new DbcStore(parameterFactory);
            containerRegistry.RegisterInstance<IDbcStore>(store);
            containerRegistry.RegisterInstance<ISpellStore>(store);
        }
    }
}
