using System.Collections.Generic;
using WDE.Common.Database;

namespace WDE.DatabaseEditors.Models
{
    /// <summary>
    /// A pending, unsaved edit of a cmangos condition tree referenced by an entity's
    /// condition_id-style column (meta type "mangos_conditions:column").
    /// </summary>
    public class MangosConditionsChange
    {
        public MangosConditionsChange(IReadOnlyList<IMangosConditionLine> lines, IReadOnlyList<uint> affectedEntries)
        {
            Lines = lines;
            AffectedEntries = affectedEntries;
        }

        /// <summary>The edited tree flattened to rows, dependency-ordered (empty = tree removed).</summary>
        public IReadOnlyList<IMangosConditionLine> Lines { get; }

        /// <summary>Every condition_entry to delete before inserting Lines (old closure + new).</summary>
        public IReadOnlyList<uint> AffectedEntries { get; }
    }
}
