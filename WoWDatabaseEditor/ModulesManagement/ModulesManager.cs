using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using WDE.Common.Services;
using WDE.Module.Attributes;
using WoWDatabaseEditorCore.CoreVersion;

namespace WoWDatabaseEditorCore.ModulesManagement
{
    public class ModulesManager : IModulesManager
    {
        private readonly ICurrentCoreSettings currentCoreSettings;
        private readonly List<ModuleData> modules = new();
        private ISet<string> ignoredModules = new HashSet<string>();
        public IEnumerable<ModuleData> Modules => modules;

        public ModulesManager(ICurrentCoreSettings currentCoreSettings)
        {
            this.currentCoreSettings = currentCoreSettings;
            if (File.Exists("ignored_modules"))
                ignoredModules = File.ReadAllLines("ignored_modules").Where(t=> !string.IsNullOrEmpty(t)).ToHashSet();
        }
        
        public void AddConflicted(Assembly conflictingAssembly, Assembly firstAssembly)
        {
            modules.Add(new ModuleData(conflictingAssembly, true, false, firstAssembly));
        }

        public bool AddModule(Assembly module)
        {
            bool enabled = ShouldLoad(module);
            modules.Add(new ModuleData(module, enabled, enabled));
            return enabled;
        }

        public bool ShouldLoad(Assembly module)
        {
            var requiredCore = module
                .GetCustomAttributes(typeof(ModuleRequiresCoreAttribute), false)
                .Cast<ModuleRequiresCoreAttribute>()
                .FirstOrDefault();
            if (requiredCore != null)
            {
                if (currentCoreSettings.CurrentCore == null)
                    return false;
                
                return requiredCore.cores.Contains(currentCoreSettings.CurrentCore);
            }
            return !ignoredModules.Contains(module.GetName().Name!);
        }
    }
}