using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using WDE.Common;
using WDE.Module.Attributes;
using WDE.TrinityMySqlDatabase.Providers;
using WDE.TrinityMySqlDatabase.ViewModels;
using WDE.TrinityMySqlDatabase.Views;

namespace WDE.TrinityMySqlDatabase
{
    [AutoRegister, SingleInstance]
    public class TrinityMySqlDatabaseConfiguration : IConfigurable
    {
        private readonly IConnectionSettingsProvider databaseSettings;

        public TrinityMySqlDatabaseConfiguration(IConnectionSettingsProvider databaseSettings)
        {
            this.databaseSettings = databaseSettings;
        }

        public KeyValuePair<ContentControl, Action> GetConfigurationView()
        {
            var view = new DatabaseConfigView();
            var viewModel = new DatabaseConfigViewModel(databaseSettings);
            view.DataContext = viewModel;
            return new KeyValuePair<ContentControl, Action>(view, viewModel.SaveAction);
        }

        public string GetName()
        {
            return "TrinityCore Database";
        }
    }
}
