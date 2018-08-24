using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Common.Services;
using WoWDatabaseEditor.Services.ConfigurationService.Views;

namespace WoWDatabaseEditor.Services.ConfigurationService
{
    [WDE.Common.Attributes.AutoRegister, WDE.Common.Attributes.SingleInstance]
    public class ConfigureService : IConfigureService
    {
        public void ShowSettings()
        {
            ConfigurationWindow view = new ConfigurationWindow();
            view.ShowDialog();
        }
    }
}
