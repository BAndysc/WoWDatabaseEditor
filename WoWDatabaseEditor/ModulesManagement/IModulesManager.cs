using System.Collections.Generic;
using System.Reflection;

namespace WoWDatabaseEditor.ModulesManagement
{
    internal interface IModulesManager
    {
        IEnumerable<ModuleData> Modules { get; }
        void AddConflicted(Assembly conflictingAssembly, Assembly firstAssembly);
        void AddModule(Assembly module);
    }

    public class ModuleData
    {
        public ModuleData(Assembly assembly, bool isLoaded, Assembly? conflictingAssembly = null)
        {
            Assembly = assembly;
            IsLoaded = isLoaded;
            ConflictingAssembly = conflictingAssembly;
        }

        public Assembly Assembly { get; }
        public bool IsLoaded { get; }
        public Assembly? ConflictingAssembly { get; }
    }
}