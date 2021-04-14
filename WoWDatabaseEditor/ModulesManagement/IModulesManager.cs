using System.Collections.Generic;
using System.Reflection;

namespace WoWDatabaseEditorCore.ModulesManagement
{
    public interface IModulesManager
    {
        IEnumerable<ModuleData> Modules { get; }
        void AddConflicted(Assembly conflictingAssembly, Assembly firstAssembly);
        bool AddModule(Assembly module);
        bool ShouldLoad(Assembly module);
    }

    public class ModuleData
    {
        public ModuleData(Assembly assembly, bool isEnabled, bool isLoaded, Assembly? conflictingAssembly = null)
        {
            Assembly = assembly;
            IsEnabled = isEnabled;
            IsLoaded = isLoaded;
            ConflictingAssembly = conflictingAssembly;
        }

        public Assembly Assembly { get; }
        public bool IsEnabled { get; }
        public bool IsLoaded { get; }
        public Assembly? ConflictingAssembly { get; }
    }
}