using System;
using WDE.Common.Database;

namespace WDE.DbScriptsEditor.Models
{
    // Verbalizes a decoded buddy locator into a full readable phrase, e.g. "all dead creatures
    // 30017 within 10 yd", "closest member of spawn group 5", "creature guid 42 (despawned)".
    // Pure logic — the editable renderer builds its own clickable markup with the same wording.
    public static class BuddyDescriptorFormatter
    {
        // nameResolver turns an (entry, isGameObject) pair into a display name (e.g. a creature
        // name); null falls back to the raw number. Only by-entry / pet locators name an entity —
        // spawn-group and string-id ids stay numeric.
        public static string Format(BuddyDescriptor buddy, Func<long, bool, string>? nameResolver = null)
        {
            if (!buddy.Provided)
                return "buddy";

            string Name(long entry) => nameResolver?.Invoke(entry, buddy.IsGameObject) ?? entry.ToString();
            var all = buddy.AllEligible && buddy.SupportsAllEligible;
            var dead = buddy.IncludeDespawned && buddy.SupportsLiveness;

            switch (buddy.Mode)
            {
                case BuddyFindMode.ByGuid:
                    return buddy.IsGameObject
                        ? $"gameobject guid {buddy.SearchValue}"
                        : $"creature guid {buddy.SearchValue}{(dead ? " (despawned)" : "")}";
                case BuddyFindMode.ByPool:
                    return $"pooled creature (pool {buddy.SearchValue}){(dead ? " (dead)" : "")}";
                case BuddyFindMode.BySpawnGroup:
                    return $"{(all ? "all members" : "closest member")} of spawn group {buddy.Entry}";
                case BuddyFindMode.ByStringId:
                {
                    var s = $"{(all ? "all objects" : "closest object")} tagged {buddy.Entry}";
                    if (dead) s += " (incl. dead)";
                    if (buddy.SearchValue > 0) s += $" within {buddy.SearchValue} yd";
                    return s;
                }
                case BuddyFindMode.Pet:
                    return $"pet {Name(buddy.Entry)}";
                default: // NearestByEntry
                {
                    var noun = buddy.IsGameObject ? (all ? "gameobjects" : "gameobject")
                                                  : (all ? "creatures" : "creature");
                    return $"{(all ? "all" : "nearest")}{(dead ? " dead" : "")} {noun} {Name(buddy.Entry)} within {buddy.SearchValue} yd";
                }
            }
        }

        // Resolves whichever of the two slots holds an actor of the wanted kind, mirroring how the
        // core picks "the player" (target-preferred) or "the involved creature" (source-preferred)
        // from the resolved source/target. Returns the actor label and whether it is the source slot
        // (so the token can edit the right slot). e.g. on-creature-death, {player} → "Killer" (the
        // player-capable target), {creature} → "Dying creature" (the source creature).
        public static (string label, bool isSource) ResolveActorOfKind(in DecodedFlags decoded,
            DbScriptTypeInfo info, DbScriptActorKind wanted, bool preferTarget,
            Func<long, bool, string>? nameResolver = null)
        {
            var buddyLabel = Format(decoded.Buddy, nameResolver);
            // A buddy is a creature or gameobject (never a player); an absent buddy contributes nothing.
            var buddyMask = decoded.Buddy.Provided
                ? (decoded.Buddy.IsGameObject ? DbScriptActorKind.GameObject : DbScriptActorKind.Creature)
                : DbScriptActorKind.None;
            (DbScriptActorKind mask, string label) Slot(SourceTargetKind kind) => kind switch
            {
                SourceTargetKind.OriginalSource => (info.SourceKinds, info.SourceLabel),
                SourceTargetKind.OriginalTarget => (info.TargetKinds, info.TargetLabel),
                _ => (buddyMask, buddyLabel),
            };
            var source = Slot(decoded.Direction.Source);
            var target = Slot(decoded.Direction.Target);
            var (first, firstIsSource) = preferTarget ? (target, false) : (source, true);
            var (second, secondIsSource) = preferTarget ? (source, true) : (target, false);
            if ((first.mask & wanted) != 0)
                return (first.label, firstIsSource);
            if ((second.mask & wanted) != 0)
                return (second.label, secondIsSource);
            return (first.label, firstIsSource); // neither clearly matches — fall back to the preferred slot
        }

        // Read-only "who acts on whom" for a raw row, resolved through the direction codec (no combo
        // table duplication). Used by the non-editable step preview.
        public static (string source, string target) ResolveActors(IDbScriptLine row, DbScriptTypeInfo info,
            DbScriptBuddyCapability buddyKind, Func<long, bool, string>? nameResolver = null)
        {
            var decoded = DbScriptFlagsCodec.Decode(row.DataFlags, row.BuddyEntry, row.SearchRadius, buddyKind);
            var buddyLabel = Format(decoded.Buddy, nameResolver);
            string Label(SourceTargetKind kind) => kind switch
            {
                SourceTargetKind.OriginalSource => info.SourceLabel,
                SourceTargetKind.OriginalTarget => info.TargetLabel,
                _ => buddyLabel,
            };
            return (Label(decoded.Direction.Source), Label(decoded.Direction.Target));
        }
    }
}
