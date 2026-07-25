using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Database;

namespace WDE.CMangosConditions.Models
{
    internal static class MangosConditionClosureLoader
    {
        /// <summary>Loads rootEntry and, transitively, every condition it references (0 = nothing).</summary>
        public static async Task<IReadOnlyList<IMangosConditionLine>> Load(IMangosDatabaseProvider databaseProvider, uint rootEntry)
        {
            var loaded = new Dictionary<uint, IMangosConditionLine>();
            var toLoad = new List<uint>();
            if (rootEntry != 0)
                toLoad.Add(rootEntry);

            while (toLoad.Count > 0)
            {
                var batch = await databaseProvider.GetConditionsByEntries(toLoad);
                toLoad.Clear();
                foreach (var line in batch)
                {
                    if (!loaded.TryAdd(line.ConditionEntry, line))
                        continue;
                    foreach (var reference in MangosConditionTreeCodec.ChildRefs(line))
                        if (!loaded.ContainsKey(reference) && !toLoad.Contains(reference))
                            toLoad.Add(reference);
                }
            }

            return loaded.Values.OrderBy(l => l.ConditionEntry).ToList();
        }
    }
}
