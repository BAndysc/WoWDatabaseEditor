using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common;
using WDE.Common.Types;
using WDE.Module.Attributes;
using WoWDatabaseEditorCore.Extensions;

namespace WoWDatabaseEditorCore.ModulesManagement.Configuration.ViewModels
{
    [AutoRegister]
    public class ModulesConfigViewModel : BindableBase, IConfigurable
    {
        public ModulesConfigViewModel(IModulesManager modulesManager)
        {
            Items = new ObservableCollection<ModuleConfigModel>();

            Items.AddRange(modulesManager.Modules.Select(m => new ModuleConfigModel(m.IsEnabled,
                m.Assembly.GetName().Name ?? "Unknown name",
                m.IsLoaded,
                GenerateDetailFor(m.ConflictingAssembly))));

            Save = new DelegateCommand(() => { });
        }

        public ObservableCollection<ModuleConfigModel> Items { get; }

        private string GenerateDetailFor(Assembly? conflictingAssembly)
        {
            if (conflictingAssembly == null)
                return "";

            #pragma warning disable IL3000
            return $"Conflicts with {conflictingAssembly.GetName().Name} ({conflictingAssembly.Location})";
            #pragma warning restore IL3000
        }

        public ImageUri Icon { get; } = new ImageUri("Icons/document_module_big.png");
        public string Name => "Modules";
        public string ShortDescription => "List of all loaded modules";
        public ICommand Save { get; }
        public bool IsModified => false;
        public bool IsRestartRequired => false;
        public ConfigurableGroup Group => ConfigurableGroup.Advanced;
    }

    public class ModuleConfigModel
    {
        public ModuleConfigModel(bool isEnabled, string name, bool isLoaded, string details)
        {
            IsEnabled = isEnabled;
            Name = name;
            IsLoaded = isLoaded;
            Details = details;
        }

        public bool IsEnabled { get; set; }
        public string Name { get; set; }
        public bool IsLoaded { get; set; }
        public string Details { get; set; }
    }
}