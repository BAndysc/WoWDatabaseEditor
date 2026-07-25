using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WDE.Common.Services
{
    /// <summary>
    /// Editor for cmangos `unit_condition` rows (8 comparison clauses combined with AND/OR).
    /// A separate system from the `conditions` tree table.
    /// </summary>
    [UniqueProvider]
    public interface IMangosUnitConditionService
    {
        /// <summary>
        /// Database mode: loads the row with the given id (null = start a new row with a
        /// suggested free negative id), lets the user edit, on accept saves to the database
        /// (delete of the old and current id + insert). Returns the saved id (may differ
        /// from the passed id when the user renumbered) or null on cancel.
        /// </summary>
        Task<int?> EditUnitCondition(int? id, string? customTitle = null);

        /// <summary>
        /// In-memory mode for embedding editors: edits the passed line (null = new line with
        /// Id preset to suggestedNewId). No database access happens here. Returns the edited
        /// line (its Id may have been changed by the user) or null on cancel.
        /// </summary>
        Task<IMangosUnitConditionLine?> EditUnitConditionInMemory(IMangosUnitConditionLine? line,
            int suggestedNewId, string? customTitle = null);

        /// <summary>One-line human readable text, e.g. "health % &lt; 20 OR is casting".</summary>
        string BuildReadable(IMangosUnitConditionLine line);

        /// <summary>Loads one row by id; null when it does not exist (or id is 0/-1).</summary>
        Task<IMangosUnitConditionLine?> LoadUnitCondition(int id);

        /// <summary>
        /// Free custom (negative) id: below the database minimum, the passed local minimum
        /// and -1 (the "no condition" sentinel), so the first suggested custom id is -2.
        /// </summary>
        Task<int> GetFreeUnitConditionId(int localMin = 0);
    }
}
