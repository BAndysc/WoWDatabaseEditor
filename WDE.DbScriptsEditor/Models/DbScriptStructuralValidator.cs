using System.Collections.Generic;

namespace WDE.DbScriptsEditor.Models
{
    // Advisory validation of a step's structural (source/target/buddy) choices against the
    // command's declared capabilities. Pure logic so it can be unit-tested. Returns human-readable
    // warnings; it never blocks editing (exotic/legacy rows must still be representable).
    public static class DbScriptStructuralValidator
    {
        public const uint RespawnCommandId = 41;

        public static IReadOnlyList<string> Validate(in DecodedFlags decoded, DbScriptCommandDefinition? command)
        {
            var warnings = new List<string>();
            var buddy = decoded.Buddy;
            var direction = decoded.Direction;

            // Bits the core ignores for this locator/command (0x400 on a fixed-kind command, a stray
            // locator bit, buddy bits on a buddy_entry == 0 row...). Kept in the raw row but inert.
            if (decoded.InertBuddyBits != 0)
                warnings.Add($"data_flags has buddy bits the core ignores here (0x{decoded.InertBuddyBits:X}).");

            if (buddy.Provided)
            {
                if (buddy.IncludeDespawned && !buddy.SupportsLiveness)
                    warnings.Add("The dead/despawned flag has no effect on this buddy locator.");
                if (buddy.AllEligible && !buddy.SupportsAllEligible)
                    warnings.Add("The 'all eligible' flag has no effect on this buddy locator (it yields a single object).");
                if (buddy.Entry == 0 && buddy.Mode is BuddyFindMode.NearestByEntry or BuddyFindMode.Pet
                        or BuddyFindMode.BySpawnGroup or BuddyFindMode.ByStringId)
                    warnings.Add("The buddy search never runs when buddy_entry is 0.");
                if (buddy.Entry != 0 && buddy.SearchValue == 0 &&
                    buddy.Mode is BuddyFindMode.NearestByEntry or BuddyFindMode.Pet)
                    warnings.Add("Buddy search radius is 0; the server skips this row at load.");
            }

            if (command != null)
            {
                if (buddy.Provided)
                {
                    if (!command.Buddy.AllowsGameObject() && buddy.IsGameObject)
                        warnings.Add("This command's buddy must be a creature, not a gameobject.");
                    else if (!command.Buddy.AllowsCreature() && !buddy.IsGameObject)
                        warnings.Add("This command's buddy must be a gameobject, not a creature.");

                    // The runtime pool branch only resolves creatures (GO pools pass the load
                    // check but the buddy is never found).
                    if (buddy.Mode == BuddyFindMode.ByPool && buddy.IsGameObject)
                        warnings.Add("Pool lookup only resolves creatures at runtime; a gameobject pool buddy is never found.");
                }

                if (command.Id == RespawnCommandId && buddy.Provided && !buddy.IncludeDespawned)
                    warnings.Add("RESPAWN usually needs 'include dead/despawned' to find the object.");

                if (direction.Target == SourceTargetKind.Buddy && IsNoneOnly(command.TargetTypes))
                    warnings.Add("This command ignores its target, so targeting the buddy has no effect.");

                // A buddy located but occupying neither slot is only meaningful for TERMINATE_SCRIPT
                // (its buddyFound fallback); elsewhere it just gates the step on the buddy existing.
                if (buddy.Provided && !direction.UsesBuddy &&
                    command.Id != DbScriptInspections.TerminateScriptCommandId)
                    warnings.Add("The buddy is used as neither source nor target; the step is skipped when it isn't found.");
            }

            return warnings;
        }

        private static bool IsNoneOnly(IReadOnlyList<string> types)
        {
            if (types.Count == 0)
                return false;
            foreach (var t in types)
            {
                if (!string.Equals(t, "None", System.StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            return true;
        }
    }
}
