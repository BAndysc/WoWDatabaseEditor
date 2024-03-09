using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Parameters
{
    public class GameObjectSpawnKeyParameter : ICustomPickerContextualParameter<long>
    {
        private readonly IDatabaseProvider databaseProvider;
        private readonly IItemFromListProvider itemFromListProvider;

        public GameObjectSpawnKeyParameter(IDatabaseProvider databaseProvider,
            IItemFromListProvider itemFromListProvider)
        {
            this.databaseProvider = databaseProvider;
            this.itemFromListProvider = itemFromListProvider;
        }

        public string? Prefix => null;

        public bool HasItems => true;

        public bool AllowUnknownItems => true;

        public string ToString(long value) => value.ToString();

        public Dictionary<long, SelectOption>? Items => null;

        public async Task<(long, bool)> PickValue(long value, object context)
        {
            if (context is not SmartBaseElement ctx)
                return (0, false);

            var entry = ctx.GetParameter(0).Value;
            var spawns = await databaseProvider.GetGameObjectsByEntryAsync((uint)entry);
            Dictionary<long, SelectOption>? dict = null;
            foreach (var s in spawns)
            {
                if (s.SpawnKey == 0)
                    continue;
                dict ??= new();
                dict[s.SpawnKey] = new SelectOption("Spawn key " + s.SpawnKey);
            }

            var result = await itemFromListProvider.GetItemFromList(dict, false, value);
            return (result ?? 0, result.HasValue);
        }

        public string ToString(long value, SmartBaseElement context) => value.ToString();
    }
}