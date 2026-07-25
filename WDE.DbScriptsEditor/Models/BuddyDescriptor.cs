using System;

namespace WDE.DbScriptsEditor.Models
{
    // How a step's buddy object is located. Mirrors the locator branches of the core's
    // ScriptAction::GetScriptProcessTargets.
    public enum BuddyFindMode
    {
        None,          // no buddy
        NearestByEntry,// nearest creature/GO with entry=buddy_entry within search_radius yards
        ByGuid,        // search_radius IS the guid; buddy_entry is the expected entry
        ByPool,        // search_radius IS the pool id; buddy_entry is the pool's entry
        BySpawnGroup,  // buddy_entry is the spawn group id; kind decided by the group itself
        ByStringId,    // object tagged with string id = buddy_entry
        Pet,           // pet of entry buddy_entry
    }

    // Immutable decode/encode of the buddy portion of a step (buddy_entry, search_radius and the
    // buddy flag bits). Never mutated in place: the editor builds a new descriptor and re-encodes.
    public readonly struct BuddyDescriptor : IEquatable<BuddyDescriptor>
    {
        public BuddyFindMode Mode { get; }
        public bool IsGameObject { get; }   // BUDDY_BY_GO — entry picker is a GO instead of a creature
        public long Entry { get; }          // buddy_entry (or expected entry for ByGuid)
        public long SearchValue { get; }    // search_radius reinterpreted per Mode (yards / guid / pool)
        public bool IncludeDespawned { get; } // BUDDY_IS_DESPAWNED
        public bool AllEligible { get; }      // ALL_ELIGIBLE_BUDDIES

        public static readonly BuddyDescriptor None = new(BuddyFindMode.None, false, 0, 0, false, false);

        public BuddyDescriptor(BuddyFindMode mode, bool isGameObject, long entry, long searchValue,
            bool includeDespawned, bool allEligible)
        {
            Mode = mode;
            IsGameObject = isGameObject;
            Entry = entry;
            SearchValue = searchValue;
            IncludeDespawned = includeDespawned;
            AllEligible = allEligible;
        }

        public bool Provided => Mode != BuddyFindMode.None;

        // Whether the alive/dead (BUDDY_IS_DESPAWNED) toggle has any effect for this locator. The
        // core only liveness-filters creature matches: creature by-entry / by-guid, pooled creatures
        // and string-id creature matches. GO locators, spawn groups and pets ignore it.
        public bool SupportsLiveness => Mode switch
        {
            BuddyFindMode.NearestByEntry => !IsGameObject,
            BuddyFindMode.ByGuid => !IsGameObject,
            BuddyFindMode.ByPool => true,
            BuddyFindMode.ByStringId => true,
            _ => false,
        };

        // Whether the closest-vs-all (ALL_ELIGIBLE_BUDDIES) toggle has any effect. GUID / pool / pet
        // always yield a single object; by-entry, spawn group and string id can yield many.
        public bool SupportsAllEligible =>
            Mode is BuddyFindMode.NearestByEntry or BuddyFindMode.BySpawnGroup or BuddyFindMode.ByStringId;

        // Whether search_radius is a yard distance (by entry / pet always, string id optionally).
        // GUID and pool reinterpret the column as an id; spawn group never reads it.
        public bool UsesRadius =>
            Mode is BuddyFindMode.NearestByEntry or BuddyFindMode.ByStringId or BuddyFindMode.Pet;

        // Reconstructs the buddy descriptor from the three physical columns + the buddy flag bits.
        // The command's buddy kind decides creature-vs-GO: fixed-kind commands ignore BUDDY_BY_GO,
        // dual commands read it. Mirrors the resolution gate in GetScriptProcessTargets — the buddy
        // block only runs when buddy_entry != 0 or the guid/pool locator bits are set, so a stray
        // spawn-group / string-id / pet / despawned bit with buddy_entry == 0 locates nothing.
        public static BuddyDescriptor Decode(uint dataFlags, long buddyEntry, long searchRadius,
            DbScriptBuddyCapability kind)
        {
            var isGo = kind switch
            {
                DbScriptBuddyCapability.GameObject => true,
                DbScriptBuddyCapability.Both => (dataFlags & DbScriptFlags.BuddyByGo) != 0,
                _ => false,
            };
            var despawned = (dataFlags & DbScriptFlags.BuddyIsDespawned) != 0;
            var all = (dataFlags & DbScriptFlags.AllEligibleBuddies) != 0;

            BuddyFindMode mode;
            if ((dataFlags & DbScriptFlags.BuddyByGuid) != 0)
                mode = BuddyFindMode.ByGuid;
            else if ((dataFlags & DbScriptFlags.BuddyByPool) != 0)
                mode = BuddyFindMode.ByPool;
            else if (buddyEntry == 0)
                return None; // no entry and no guid/pool locator → the core searches nothing
            else if ((dataFlags & DbScriptFlags.BuddyBySpawnGroup) != 0)
                mode = BuddyFindMode.BySpawnGroup;
            else if ((dataFlags & DbScriptFlags.BuddyByStringId) != 0)
                mode = BuddyFindMode.ByStringId;
            else if ((dataFlags & DbScriptFlags.BuddyIsPet) != 0)
                mode = BuddyFindMode.Pet;
            else
                mode = BuddyFindMode.NearestByEntry;

            return new BuddyDescriptor(mode, isGo, buddyEntry, searchRadius, despawned, all);
        }

        // Produces (buddy flag bits, buddy_entry, search_radius). The direction/command bits are
        // added separately by the codec; here we only own the 0xFF0 buddy region. BUDDY_BY_GO is
        // emitted only for dual (Both) commands — for fixed-kind commands the kind is implicit, so
        // emitting the bit would be a no-op the core ignores.
        public (uint flagBits, long buddyEntry, long searchRadius) Encode(DbScriptBuddyCapability kind)
        {
            if (Mode == BuddyFindMode.None)
                return (0, 0, 0);

            uint bits = Mode switch
            {
                BuddyFindMode.ByGuid => DbScriptFlags.BuddyByGuid,
                BuddyFindMode.ByPool => DbScriptFlags.BuddyByPool,
                BuddyFindMode.BySpawnGroup => DbScriptFlags.BuddyBySpawnGroup,
                BuddyFindMode.ByStringId => DbScriptFlags.BuddyByStringId,
                BuddyFindMode.Pet => DbScriptFlags.BuddyIsPet,
                _ => 0, // NearestByEntry has no locator bit
            };
            if (kind == DbScriptBuddyCapability.Both && IsGameObject)
                bits |= DbScriptFlags.BuddyByGo;
            if (IncludeDespawned)
                bits |= DbScriptFlags.BuddyIsDespawned;
            if (AllEligible)
                bits |= DbScriptFlags.AllEligibleBuddies;

            return (bits, Entry, SearchValue);
        }

        public bool Equals(BuddyDescriptor other) =>
            Mode == other.Mode && IsGameObject == other.IsGameObject && Entry == other.Entry &&
            SearchValue == other.SearchValue && IncludeDespawned == other.IncludeDespawned &&
            AllEligible == other.AllEligible;

        public override bool Equals(object? obj) => obj is BuddyDescriptor o && Equals(o);
        public override int GetHashCode() =>
            HashCode.Combine((int)Mode, IsGameObject, Entry, SearchValue, IncludeDespawned, AllEligible);
    }
}
