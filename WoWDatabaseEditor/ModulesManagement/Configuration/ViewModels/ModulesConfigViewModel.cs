using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Prism.Mvvm;
using WDE.Module.Attributes;

namespace WoWDatabaseEditor.ModulesManagement.Configuration.ViewModels
{
    [AutoRegister]
    internal class ModulesConfigViewModel : BindableBase
    {
        public ModulesConfigViewModel(IModulesManager modulesManager)
        {
            Items = new ObservableCollection<ModuleConfigModel>();

            Items.AddRange(modulesManager.Modules.Select(m => new ModuleConfigModel(true,
                m.Assembly.GetName().Name ?? "Unknown name",
                m.IsLoaded,
                GenerateDetailFor(m.ConflictingAssembly))));
        }

        public ObservableCollection<ModuleConfigModel> Items { get; }

        private string GenerateDetailFor(Assembly? conflictingAssembly)
        {
            if (conflictingAssembly == null)
                return "";

            return $"Conflicts with {conflictingAssembly.GetName().Name} ({conflictingAssembly.Location})";
        }
    }

    internal class ModuleConfigModel
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