using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Common.Services;
using WoWDatabaseEditor.Services.ConfigurationService.Views;
using WoWDatabaseEditor.Views;

namespace WoWDatabaseEditor.Services.ConfigurationService
{
    [WDE.Module.Attributes.AutoRegister, WDE.Module.Attributes.SingleInstance]
    public class ConfigureService : IConfigureService
    {
        public ConfigureService()
        {
        }

        public void ShowSettings()
        {
            ConfigurationWindow view = new ConfigurationWindow();
            view.ShowDialog();
        }
    }
}
