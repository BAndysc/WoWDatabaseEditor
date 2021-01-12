using WDE.Common.Services;
using WDE.Module.Attributes;
using WoWDatabaseEditor.Services.ConfigurationService.Views;

namespace WoWDatabaseEditor.Services.ConfigurationService
{
    [AutoRegister]
    [SingleInstance]
    public class ConfigureService : IConfigureService
    {
        public void ShowSettings()
        {
            ConfigurationWindow view = new();
            view.ShowDialog();
        }
    }
}