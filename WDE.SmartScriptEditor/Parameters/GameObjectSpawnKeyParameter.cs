using System.Collections.Generic;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Parameters
{
    public class GameObjectSpawnKeyParameter : IContextualParameter<long, SmartBaseElement>
    {
        private readonly IDatabaseProvider databaseProvider;

        public GameObjectSpawnKeyParameter(IDatabaseProvider databaseProvider)
        {
            this.databaseProvider = databaseProvider;
        }
        
        public bool HasItems => true;
        
        public string ToString(long value) => value.ToString();

        public Dictionary<long, SelectOption>? Items => null;

        public Dictionary<long, SelectOption>? ItemsForContext(SmartBaseElement context)
        {
            var entry = context.GetParameter(0).Value;
            var spawns = databaseProvider.GetGameObjectsByEntry((uint)entry);
            Dictionary<long, SelectOption>? dict = null;
            foreach (var s in spawns)
            {
                if (s.SpawnKey == 0)
                    continue;
                dict ??= new();
                dict[s.SpawnKey] = new SelectOption("Spawn key " + s.SpawnKey);
            }
            return dict;
        }

        public string ToString(long value, SmartBaseElement context) => value.ToString();
    }
}