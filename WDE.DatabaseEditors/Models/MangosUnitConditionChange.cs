using System.Collections.Generic;
using WDE.Common.Database;

namespace WDE.DatabaseEditors.Models
{
    /// <summary>
    /// A pending, unsaved edit of a cmangos unit_condition row referenced by an entity's
    /// id column (meta type "mangos_unit_conditions:column").
    /// </summary>
    public class MangosUnitConditionChange
    {
        public MangosUnitConditionChange(IMangosUnitConditionLine line, IReadOnlyList<int> affectedIds)
        {
            Line = line;
            AffectedIds = affectedIds;
        }

        /// <summary>The edited row.</summary>
        public IMangosUnitConditionLine Line { get; }

        /// <summary>Every unit_condition.Id to delete before inserting Line (old ids + new).</summary>
        public IReadOnlyList<int> AffectedIds { get; }
    }
}
