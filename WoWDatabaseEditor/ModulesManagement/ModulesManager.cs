using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using WDE.Common.Services;

namespace WoWDatabaseEditorCore.ModulesManagement
{
    public class ModulesManager : IModulesManager
    {
        private readonly List<ModuleData> modules = new();
        private ISet<string> ignoredModules = new HashSet<string>();
        public IEnumerable<ModuleData> Modules => modules;

        public ModulesManager()
        {
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
            return !ignoredModules.Contains(module.GetName().Name!);
        }
    }
}