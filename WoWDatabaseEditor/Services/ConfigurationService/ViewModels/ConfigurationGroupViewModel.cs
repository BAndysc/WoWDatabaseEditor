using System.Collections.ObjectModel;
using WDE.Common;

namespace WoWDatabaseEditorCore.Services.ConfigurationService.ViewModels
{
    public class ConfigurationGroupViewModel : ObservableCollection<IConfigurable>
    {
        public ConfigurationGroupViewModel(ConfigurableGroup group)
        {
            Group = group;
            GroupName = group.ToString();
        }

        public ConfigurableGroup Group { get; }
        public string GroupName { get; }
    }
}