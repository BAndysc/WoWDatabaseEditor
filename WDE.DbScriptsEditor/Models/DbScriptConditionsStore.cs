using System.Collections.Generic;
using System.Linq;
using WDE.Common.Database;

namespace WDE.DbScriptsEditor.Models
{
    /// <summary>
    /// The `conditions` rows a dbscript document knows about: everything loaded for the
    /// script's condition_id references plus the results of in-editor condition edits.
    /// Edits stay in memory; their SQL (delete + reinsert of the affected entries) is
    /// bundled into the script's query on save/export.
    /// </summary>
    public class DbScriptConditionsStore
    {
        private readonly Dictionary<uint, IMangosConditionLine> lines = new();
        private readonly HashSet<uint> affected = new();

        /// <summary>Adds freshly loaded (not dirty) rows.</summary>
        public void AddLoaded(IReadOnlyList<IMangosConditionLine> loaded)
        {
            foreach (var line in loaded)
                if (line.ConditionEntry != 0)
                    lines[line.ConditionEntry] = line;
        }

        /// <summary>
        /// Fetches the given roots and, transitively, everything they reference through the
        /// passed fetch function, skipping rows already known.
        /// </summary>
        public async System.Threading.Tasks.Task LoadClosuresAsync(IEnumerable<uint> roots,
            System.Func<IReadOnlyList<uint>, System.Threading.Tasks.Task<IReadOnlyList<IMangosConditionLine>>> fetch)
        {
            var seen = new HashSet<uint>(lines.Keys);
            var toLoad = roots.Where(r => r != 0 && seen.Add(r)).ToList();
            while (toLoad.Count > 0)
            {
                var batch = await fetch(toLoad);
                toLoad.Clear();
                foreach (var line in batch)
                {
                    if (line.ConditionEntry == 0)
                        continue;
                    lines[line.ConditionEntry] = line;
                    foreach (var reference in ChildRefs(line))
                        if (seen.Add(reference))
                            toLoad.Add(reference);
                }
            }
        }

        /// <summary>rootEntry's row plus everything it transitively references (empty for 0/unknown).</summary>
        public IReadOnlyList<IMangosConditionLine> Closure(uint rootEntry)
        {
            var result = new List<IMangosConditionLine>();
            var seen = new HashSet<uint>();
            var queue = new Queue<uint>();
            if (rootEntry != 0 && seen.Add(rootEntry))
                queue.Enqueue(rootEntry);
            while (queue.Count > 0)
            {
                if (!lines.TryGetValue(queue.Dequeue(), out var line))
                    continue;
                result.Add(line);
                foreach (var reference in ChildRefs(line))
                    if (seen.Add(reference))
                        queue.Enqueue(reference);
            }
            return result;
        }

        /// <summary>
        /// Applies an edit session: the entries that made up the tree before the edit are
        /// replaced by the result rows. Entries the user removed from the tree get deleted
        /// on save; everything involved becomes part of the exported SQL.
        /// </summary>
        public void ApplyEdit(IEnumerable<uint> previousEntries, IReadOnlyList<IMangosConditionLine> result)
        {
            foreach (var entry in previousEntries)
            {
                affected.Add(entry);
                lines.Remove(entry);
            }
            foreach (var line in result)
            {
                affected.Add(line.ConditionEntry);
                lines[line.ConditionEntry] = line;
            }
        }

        public bool HasChanges => affected.Count > 0;

        /// <summary>Entries to DELETE on save (edited, replaced or removed).</summary>
        public IReadOnlyList<uint> AffectedEntries => affected.OrderBy(x => x).ToList();

        /// <summary>Rows to (re)INSERT on save, dependency-ordered by entry.</summary>
        public IReadOnlyList<IMangosConditionLine> AffectedLines =>
            affected.Where(lines.ContainsKey).OrderBy(x => x).Select(x => lines[x]).ToList();

        public uint MaxEntry => lines.Count == 0 ? 0 : lines.Keys.Max();

        public bool Knows(uint entry) => lines.ContainsKey(entry);

        private static IEnumerable<uint> ChildRefs(IMangosConditionLine line)
        {
            // -3 NOT references value1; -2 OR / -1 AND reference value1..4
            if (line.ConditionType == -3)
            {
                if (line.Value1 != 0)
                    yield return line.Value1;
            }
            else if (line.ConditionType is -1 or -2)
            {
                if (line.Value1 != 0)
                    yield return line.Value1;
                if (line.Value2 != 0)
                    yield return line.Value2;
                if (line.Value3 != 0)
                    yield return line.Value3;
                if (line.Value4 != 0)
                    yield return line.Value4;
            }
        }
    }
}
