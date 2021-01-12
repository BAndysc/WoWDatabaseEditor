using System;
using System.Collections.Generic;
using System.Windows.Controls;
using WDE.Common;
using WDE.DbcStore.Providers;
using WDE.DbcStore.ViewModels;
using WDE.DbcStore.Views;
using WDE.Module.Attributes;

namespace WDE.Parameters
{
    [AutoRegister]
    [SingleInstance]
    public class DbcStoreConfiguration : IConfigurable
    {
        private readonly IDbcSettingsProvider dbcSettingsProvider;

        public DbcStoreConfiguration(IDbcSettingsProvider dbcSettingsProvider)
        {
            this.dbcSettingsProvider = dbcSettingsProvider;
        }

        public KeyValuePair<ContentControl, Action> GetConfigurationView()
        {
            DBCConfigView view = new DBCConfigView();
            DBCConfigViewModel viewModel = new DBCConfigViewModel(dbcSettingsProvider);
            view.DataContext = viewModel;
            return new KeyValuePair<ContentControl, Action>(view, viewModel.SaveAction);
        }

        public string GetName()
        {
            return "DBC";
        }
    }
}