using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WDE.Common.Services
{
    /// <summary>
    /// What the core will pass as the condition's `source` and `target` objects when it
    /// evaluates the condition — caller-specific (e.g. gossip option: target = the player,
    /// source = the NPC/GO the menu belongs to; dbscripts: the step's resolved pair after
    /// buddy resolution). Lets the editor display real actor names and offer the
    /// swap-source-and-target flag (0x2) as a "checked on" choice.
    /// </summary>
    public class MangosConditionSourceTarget
    {
        public MangosConditionSourceTarget(string target, string? source)
        {
            Target = target;
            Source = source;
        }

        /// <summary>The object conditions are checked on by default.</summary>
        public string Target { get; }

        /// <summary>null = the core passes no source object for this condition user.</summary>
        public string? Source { get; }
    }

    /// <summary>
    /// Editor for cmangos `conditions` trees (AND/OR/NOT nodes with leaf conditions).
    /// </summary>
    [UniqueProvider]
    public interface IMangosConditionService
    {
        /// <summary>
        /// Pure in-memory mode: pass condition lines (roots plus every entry they reference),
        /// get the full edited set back, dependency-ordered with entries assigned
        /// (new nodes receive ids above the highest passed entry). Nothing is loaded from
        /// or saved to the database. Returns null when the user cancels.
        /// </summary>
        Task<IReadOnlyList<IMangosConditionLine>?> EditConditions(IReadOnlyList<IMangosConditionLine> conditions, string? customTitle = null,
            MangosConditionSourceTarget? sourceTarget = null);

        /// <summary>
        /// Database mode: loads the closure of rootEntry (0 = start with an empty editor),
        /// lets the user edit, and on accept saves to the database (delete + reinsert of the
        /// edited entries; new nodes get MAX(condition_entry)+1). Returns the root's entry id
        /// (which may differ from rootEntry when ids had to be reassigned) or null on cancel.
        /// </summary>
        Task<uint?> EditConditions(uint rootEntry, string? customTitle = null,
            MangosConditionSourceTarget? sourceTarget = null);

        /// <summary>
        /// In-memory single-tree mode for embedding editors (e.g. dbscripts): edits the tree
        /// rooted at rootEntry (0 = create a new condition), using knownConditions as the pool
        /// of already-known rows. Exactly one root is enforced. New nodes get ids starting at
        /// firstFreeEntry (the caller must pass a value above every entry it knows about,
        /// including the database maximum, if the result will be saved later).
        /// Returns the edited closure plus the (possibly new) root id, or null on cancel.
        /// No database access happens here.
        /// </summary>
        Task<MangosConditionTreeEditResult?> EditConditionTree(uint rootEntry, IReadOnlyList<IMangosConditionLine> knownConditions,
            uint firstFreeEntry, string? customTitle = null, MangosConditionSourceTarget? sourceTarget = null);

        /// <summary>
        /// One-line human readable text of the condition tree rooted at rootEntry, resolved
        /// against the passed rows (e.g. "quest Foo rewarded AND NOT has aura Bar").
        /// When sourceTarget is given, actor names are substituted into the text.
        /// </summary>
        string BuildReadable(uint rootEntry, IReadOnlyList<IMangosConditionLine> knownConditions,
            MangosConditionSourceTarget? sourceTarget = null);

        /// <summary>
        /// Loads rootEntry's row and, transitively, every condition it references from the
        /// database (empty for 0). No editor is shown and nothing is saved.
        /// </summary>
        Task<IReadOnlyList<IMangosConditionLine>> LoadConditionsClosure(uint rootEntry);

        /// <summary>
        /// First condition_entry safe for new rows: above both the database maximum
        /// and the passed local maximum.
        /// </summary>
        Task<uint> GetFirstFreeConditionEntry(uint localMax);
    }

    public class MangosConditionTreeEditResult
    {
        public MangosConditionTreeEditResult(IReadOnlyList<IMangosConditionLine> lines, uint rootEntry)
        {
            Lines = lines;
            RootEntry = rootEntry;
        }

        /// <summary>The edited tree flattened to rows, dependency-ordered, entries assigned.</summary>
        public IReadOnlyList<IMangosConditionLine> Lines { get; }

        /// <summary>Entry of the tree root (may differ from the entry passed for editing).</summary>
        public uint RootEntry { get; }
    }
}
