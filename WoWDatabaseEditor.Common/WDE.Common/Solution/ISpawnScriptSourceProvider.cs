using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Solution
{
    /// <summary>A creature or gameobject spawn a script can be attached to.</summary>
    public readonly record struct SpawnScriptOwner(bool IsCreature, uint Entry, uint Guid);

    /// <summary>
    /// One script "slot" of a spawn: a place a script can live for this entity on the current core
    /// (e.g. smart_scripts by entry, creature_ai_scripts, dbscripts_on_creature_death). A slot is
    /// reported whether or not a script row exists yet, so the UI can offer "edit" or "add".
    /// </summary>
    public sealed class SpawnScriptSlot
    {
        /// <summary>Short human name, e.g. "Smart script", "EventAI", "On death".</summary>
        public required string Name { get; init; }

        /// <summary>Optional extra info shown next to the name, e.g. "12 lines".</summary>
        public string? Detail { get; init; }

        /// <summary>True when at least one script row exists in the database.</summary>
        public bool Exists { get; init; }

        /// <summary>Opening this item shows the right editor; null = informational slot only.</summary>
        public ISolutionItem? SolutionItem { get; init; }

        /// <summary>Slots from all providers are sorted by this before display.</summary>
        public int Order { get; init; }
    }

    /// <summary>
    /// Enumerates the script slots of a spawn. Implemented by each script editor module
    /// (SmartScript, EventAI, dbscripts...), so which slots exist is decided per core by module
    /// gating / core feature flags — consumers stay free of core conditionals. Inject
    /// IEnumerable&lt;ISpawnScriptSourceProvider&gt; and concatenate the results.
    /// </summary>
    [NonUniqueProvider]
    public interface ISpawnScriptSourceProvider
    {
        Task<IReadOnlyList<SpawnScriptSlot>> GetScripts(SpawnScriptOwner owner);

        /// <summary>The editor item for a waypoint movement script (e.g. cmangos
        /// dbscripts_on_creature_movement, keyed by the waypoint row's script id, not by the spawn).
        /// Null = this source has no movement scripts.</summary>
        Task<ISolutionItem?> CreateMovementScriptItem(uint scriptId) => Task.FromResult<ISolutionItem?>(null);

        /// <summary>A not-yet-used movement script id, for attaching a brand new script to a
        /// waypoint. Null = this source has no movement scripts.</summary>
        Task<uint?> SuggestFreeMovementScriptId() => Task.FromResult<uint?>(null);
    }
}
