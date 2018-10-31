using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using WDE.Common;
using WDE.Module.Attributes;
using WDE.DbcStore.Providers;
using WDE.DbcStore.ViewModels;
using WDE.DbcStore.Views;

namespace WDE.Parameters
{
    [AutoRegister, SingleInstance]
    public class DbcStoreConfiguration : IConfigurable
    {
        private readonly IDbcSettingsProvider dbcSettingsProvider;

        public DbcStoreConfiguration(IDbcSettingsProvider dbcSettingsProvider)
        {
            this.dbcSettingsProvider = dbcSettingsProvider;
        }

        public KeyValuePair<ContentControl, Action> GetConfigurationView()
        {
            var view = new DBCConfigView();
            var viewModel = new DBCConfigViewModel(dbcSettingsProvider);
            view.DataContext = viewModel;
            return new KeyValuePair<ContentControl, Action>(view, viewModel.SaveAction);
        }

        public string GetName()
        {
            return "DBC";
        }
    }
}
