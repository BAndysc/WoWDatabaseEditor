using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WoWDatabaseEditor.ModulesManagement
{
    interface IModulesManager
    {
        void AddConflicted(Assembly conflictingAssembly, Assembly firstAssembly);
        void AddModule(Assembly module);
        IEnumerable<ModuleData> Modules { get; }
    }

    public class ModuleData
    {
        public ModuleData(Assembly assembly, bool isLoaded, Assembly conflictingAssembly = null)
        {
            Assembly = assembly;
            IsLoaded = isLoaded;
            ConflictingAssembly = conflictingAssembly;
        }

        public Assembly Assembly { get; }
        public bool IsLoaded { get; }
        public Assembly ConflictingAssembly { get; }
    }
}
