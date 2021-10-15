using System.Collections.Generic;
using System.Linq;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.CustomCommands
{
    [AutoRegister]
    [SingleInstance]
    public class DatabaseTableCommandService : IDatabaseTableCommandService
    {
        private Dictionary<string, IDatabaseTableCommand> registry = new();

        public DatabaseTableCommandService(IEnumerable<IDatabaseTableCommand> commands)
        {
            registry = commands.ToDictionary(c => c.CommandId, c => c);
        }

        public IDatabaseTableCommand? FindCommand(string id)
        {
            if (registry.TryGetValue(id, out var cmd))
                return cmd;
            return null;
        }
    }
}