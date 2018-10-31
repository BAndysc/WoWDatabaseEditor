using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WoWDatabaseEditor.ModulesManagement
{
    class ModulesManager : IModulesManager
    {
        private List<ModuleData> modules = new List<ModuleData>();

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
