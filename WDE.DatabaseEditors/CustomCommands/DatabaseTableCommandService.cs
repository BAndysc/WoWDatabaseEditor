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
        private Dictionary<string, IDatabaseTablePerKeyCommand> registryPerKey = new();

        public DatabaseTableCommandService(IEnumerable<IDatabaseTableCommand> commands,
            IEnumerable<IDatabaseTablePerKeyCommand> commandsPerKey)
        {
            registry = commands.ToDictionary(c => c.CommandId, c => c);
            registryPerKey = commandsPerKey.ToDictionary(c => c.CommandId, c => c);
        }

        public IDatabaseTableCommand? FindCommand(string id)
        {
            if (registry.TryGetValue(id, out var cmd))
                return cmd;
            return null;
        }

        public IDatabaseTablePerKeyCommand? FindPerKeyCommand(string id)
        {
            if (registryPerKey.TryGetValue(id, out var cmd))
                return cmd;
            return null;
        }
    }
}