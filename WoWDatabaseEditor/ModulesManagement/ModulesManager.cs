using System.Collections.Generic;
using System.Reflection;

namespace WoWDatabaseEditor.ModulesManagement
{
    internal class ModulesManager : IModulesManager
    {
        private readonly List<ModuleData> modules = new();

        public IEnumerable<ModuleData> Modules => modules;

        public void AddConflicted(Assembly conflictingAssembly, Assembly firstAssembly)
        {
            modules.Add(new ModuleData(conflictingAssembly, false, firstAssembly));
        }

        public void AddModule(Assembly module)
        {
            modules.Add(new ModuleData(module, true));
        }
    }
}