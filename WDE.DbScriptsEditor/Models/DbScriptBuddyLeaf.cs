using System.Collections.Generic;

namespace WDE.DbScriptsEditor.Models
{
    // What follow-up value a buddy leaf needs the user to supply, and which physical column it
    // lands in. Entry prompts fill buddy_entry; search prompts fill search_radius.
    public enum LeafPrompt
    {
        None,
        CreatureEntry,    // buddy_entry = creature id (creature entity picker)
        GameObjectEntry,  // buddy_entry = gameobject id (GO entity picker)
        SpawnGroupId,     // buddy_entry = spawn group id (number)
        StringId,         // buddy_entry = string id (number)
        Guid,             // search_radius = spawn guid (number)
        PoolId,           // search_radius = pool id (number)
        RadiusYd,         // search_radius = radius in yards (number)
        OptionalRadiusYd, // search_radius = max distance in yards, 0 = unlimited (number)
    }

    // One user-pickable "leaf" of the core's buddy taxonomy — a fully-specified way to locate a
    // buddy object (locator × creature/GO × alive/dead × closest/all). The tile list is exactly
    // this set, filtered by the command's buddy kind and the slot's actor kind, so the picker can
    // never offer an impossible combination nor miss a possible one.
    public sealed record DbScriptBuddyLeaf(
        string Id,
        string Label,
        string Group,
        string Help,
        string SearchTags,
        BuddyFindMode Mode,
        bool IsGameObject,
        bool IncludeDespawned,
        bool AllEligible,
        DbScriptActorKind KindMask,
        LeafPrompt EntryPrompt,
        LeafPrompt SearchPrompt)
    {
        // Whether this leaf is meaningful for a command declaring the given buddy kind. Pool and pet
        // are creature-only in the core; spawn-group and string-id resolve either kind (the group /
        // tag decides), so they are offered for every command; by-entry/by-guid follow the kind.
        public bool ValidFor(DbScriptBuddyCapability cap) => Mode switch
        {
            BuddyFindMode.BySpawnGroup => true,
            BuddyFindMode.ByStringId => true,
            BuddyFindMode.ByPool => cap.AllowsCreature(),
            BuddyFindMode.Pet => cap.AllowsCreature(),
            _ => IsGameObject ? cap.AllowsGameObject() : cap.AllowsCreature(),
        };

        public BuddyDescriptor ToDescriptor(long entry, long searchValue) =>
            new(Mode, IsGameObject, entry, searchValue, IncludeDespawned, AllEligible);
    }

    public static class DbScriptBuddyLeaves
    {
        private const string EntryCreature = "By entry (creature)";
        private const string EntryGo = "By entry (gameobject)";
        private const string ByGuid = "By GUID";
        private const string ByPool = "By pool";
        private const string SpawnGroup = "Spawn group";
        private const string StringId = "By string id";
        private const string PetGroup = "Pet";

        private const DbScriptActorKind Both = DbScriptActorKind.Creature | DbScriptActorKind.GameObject;

        public static readonly IReadOnlyList<DbScriptBuddyLeaf> All = new[]
        {
            // ---- by entry, creature ----
            new DbScriptBuddyLeaf("entry_creature_nearest_alive", "Nearest creature (by entry)", EntryCreature,
                "The nearest alive creature of a given entry within a search radius.",
                "buddy nearest npc creature alive entry",
                BuddyFindMode.NearestByEntry, false, false, false, DbScriptActorKind.Creature,
                LeafPrompt.CreatureEntry, LeafPrompt.RadiusYd),
            new DbScriptBuddyLeaf("entry_creature_nearest_dead", "Nearest dead/despawned creature (by entry)", EntryCreature,
                "The nearest dead or despawned creature of a given entry within a search radius.",
                "buddy nearest npc creature dead despawned entry",
                BuddyFindMode.NearestByEntry, false, true, false, DbScriptActorKind.Creature,
                LeafPrompt.CreatureEntry, LeafPrompt.RadiusYd),
            new DbScriptBuddyLeaf("entry_creature_all_alive", "All alive creatures (by entry)", EntryCreature,
                "Every alive creature of a given entry within a search radius (the step runs for each).",
                "buddy all npc creature alive entry multi",
                BuddyFindMode.NearestByEntry, false, false, true, DbScriptActorKind.Creature,
                LeafPrompt.CreatureEntry, LeafPrompt.RadiusYd),
            new DbScriptBuddyLeaf("entry_creature_all_dead", "All dead/despawned creatures (by entry)", EntryCreature,
                "Every dead or despawned creature of a given entry within a search radius.",
                "buddy all npc creature dead despawned entry multi",
                BuddyFindMode.NearestByEntry, false, true, true, DbScriptActorKind.Creature,
                LeafPrompt.CreatureEntry, LeafPrompt.RadiusYd),

            // ---- by entry, gameobject (no liveness) ----
            new DbScriptBuddyLeaf("entry_go_nearest", "Nearest gameobject (by entry)", EntryGo,
                "The nearest gameobject of a given entry within a search radius.",
                "buddy nearest go gameobject object entry",
                BuddyFindMode.NearestByEntry, true, false, false, DbScriptActorKind.GameObject,
                LeafPrompt.GameObjectEntry, LeafPrompt.RadiusYd),
            new DbScriptBuddyLeaf("entry_go_all", "All gameobjects (by entry)", EntryGo,
                "Every gameobject of a given entry within a search radius (the step runs for each).",
                "buddy all go gameobject object entry multi",
                BuddyFindMode.NearestByEntry, true, false, true, DbScriptActorKind.GameObject,
                LeafPrompt.GameObjectEntry, LeafPrompt.RadiusYd),

            // ---- pet ----
            new DbScriptBuddyLeaf("pet", "Pet (by entry)", PetGroup,
                "The nearest alive pet of a given entry near the original actor.",
                "buddy pet creature",
                BuddyFindMode.Pet, false, false, false, DbScriptActorKind.Creature,
                LeafPrompt.CreatureEntry, LeafPrompt.RadiusYd),
            new DbScriptBuddyLeaf("pet_all", "All pets (by entry)", PetGroup,
                "Every alive pet of a given entry within a search radius (the step runs for each).",
                "buddy pet creature all multi",
                BuddyFindMode.Pet, false, false, true, DbScriptActorKind.Creature,
                LeafPrompt.CreatureEntry, LeafPrompt.RadiusYd),

            // ---- by guid ----
            new DbScriptBuddyLeaf("guid_creature_alive", "Creature by GUID (alive)", ByGuid,
                "A specific spawned creature located by its spawn GUID; expected alive.",
                "buddy guid npc creature alive",
                BuddyFindMode.ByGuid, false, false, false, DbScriptActorKind.Creature,
                LeafPrompt.None, LeafPrompt.Guid),
            new DbScriptBuddyLeaf("guid_creature_dead", "Creature by GUID (dead/despawned)", ByGuid,
                "A specific spawned creature located by its spawn GUID; expected dead or despawned.",
                "buddy guid npc creature dead despawned",
                BuddyFindMode.ByGuid, false, true, false, DbScriptActorKind.Creature,
                LeafPrompt.None, LeafPrompt.Guid),
            new DbScriptBuddyLeaf("guid_go", "Gameobject by GUID", ByGuid,
                "A specific spawned gameobject located by its spawn GUID.",
                "buddy guid go gameobject object",
                BuddyFindMode.ByGuid, true, false, false, DbScriptActorKind.GameObject,
                LeafPrompt.None, LeafPrompt.Guid),

            // ---- by pool (creature-only in the core) ----
            new DbScriptBuddyLeaf("pool_alive", "Pooled creature (alive)", ByPool,
                "The currently spawned alive creature of a spawn pool.",
                "buddy pool creature alive",
                BuddyFindMode.ByPool, false, false, false, DbScriptActorKind.Creature,
                LeafPrompt.None, LeafPrompt.PoolId),
            new DbScriptBuddyLeaf("pool_dead", "Pooled creature (dead/despawned)", ByPool,
                "The dead or despawned creature of a spawn pool.",
                "buddy pool creature dead despawned",
                BuddyFindMode.ByPool, false, true, false, DbScriptActorKind.Creature,
                LeafPrompt.None, LeafPrompt.PoolId),

            // ---- spawn group (kind decided by the group; no liveness, no radius) ----
            new DbScriptBuddyLeaf("spawngroup_closest", "Closest spawn group member", SpawnGroup,
                "The closest spawned member of a spawn group.",
                "buddy spawn group closest member",
                BuddyFindMode.BySpawnGroup, false, false, false, Both,
                LeafPrompt.SpawnGroupId, LeafPrompt.None),
            new DbScriptBuddyLeaf("spawngroup_all", "All spawn group members", SpawnGroup,
                "Every spawned member of a spawn group (the step runs for each).",
                "buddy spawn group all members multi",
                BuddyFindMode.BySpawnGroup, false, false, true, Both,
                LeafPrompt.SpawnGroupId, LeafPrompt.None),

            // ---- by string id (matches creatures and GOs; liveness filters creatures only) ----
            new DbScriptBuddyLeaf("stringid_closest_alive", "Closest object by string id (alive)", StringId,
                "The closest object tagged with a string id; dead creatures excluded.",
                "buddy string id tag closest alive",
                BuddyFindMode.ByStringId, false, false, false, Both,
                LeafPrompt.StringId, LeafPrompt.OptionalRadiusYd),
            new DbScriptBuddyLeaf("stringid_closest_dead", "Closest object by string id (dead)", StringId,
                "The closest object tagged with a string id; gameobjects always match, creatures only when dead/despawned.",
                "buddy string id tag closest dead despawned",
                BuddyFindMode.ByStringId, false, true, false, Both,
                LeafPrompt.StringId, LeafPrompt.OptionalRadiusYd),
            new DbScriptBuddyLeaf("stringid_all_alive", "All objects by string id (alive)", StringId,
                "Every object tagged with a string id; dead creatures excluded (the step runs for each).",
                "buddy string id tag all alive multi",
                BuddyFindMode.ByStringId, false, false, true, Both,
                LeafPrompt.StringId, LeafPrompt.OptionalRadiusYd),
            new DbScriptBuddyLeaf("stringid_all_dead", "All objects by string id (dead)", StringId,
                "Every object tagged with a string id; gameobjects always match, creatures only when dead/despawned.",
                "buddy string id tag all dead despawned multi",
                BuddyFindMode.ByStringId, false, true, true, Both,
                LeafPrompt.StringId, LeafPrompt.OptionalRadiusYd),
        };

        // The leaf a decoded buddy corresponds to, for preselecting the tile. Liveness / all-eligible
        // only distinguish leaves where the locator actually honours them; creature-vs-GO only for
        // by-entry / by-guid (spawn group and string id are kind-agnostic).
        public static DbScriptBuddyLeaf? Match(BuddyDescriptor b)
        {
            if (!b.Provided)
                return null;
            foreach (var leaf in All)
            {
                if (leaf.Mode != b.Mode)
                    continue;
                if (b.SupportsLiveness && leaf.IncludeDespawned != b.IncludeDespawned)
                    continue;
                if (b.SupportsAllEligible && leaf.AllEligible != b.AllEligible)
                    continue;
                if ((b.Mode == BuddyFindMode.NearestByEntry || b.Mode == BuddyFindMode.ByGuid) &&
                    leaf.IsGameObject != b.IsGameObject)
                    continue;
                return leaf;
            }
            return null;
        }

        public static int IndexOf(DbScriptBuddyLeaf leaf)
        {
            for (var i = 0; i < All.Count; i++)
                if (ReferenceEquals(All[i], leaf))
                    return i;
            return -1;
        }
    }
}
