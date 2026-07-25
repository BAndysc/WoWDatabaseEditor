using System.Collections.Generic;

namespace WDE.DbScriptsEditor.Models
{
    // Advisory validation of a step's structural (source/target/buddy) choices against the
    // command's declared capabilities. Pure logic so it can be unit-tested. Returns human-readable
    // warnings; it never blocks editing (exotic/legacy rows must still be representable).
    public static class DbScriptStructuralValidator
    {
        public const uint RespawnCommandId = 41;

        public static IReadOnlyList<string> Validate(
            ScriptDirection direction, BuddyDescriptor buddy, DbScriptCommandDefinition? command)
        {
            var warnings = new List<string>();

            if (buddy.Mode == BuddyFindMode.BySpawnGroup)
                warnings.Add("Spawn-group buddy lookup is not implemented in the core (NYI).");

            if (command != null)
            {
                if (buddy.Provided)
                {
                    if (!command.Buddy.AllowsGameObject() && buddy.IsGameObject)
                        warnings.Add("This command's buddy must be a creature, not a gameobject.");
                    else if (!command.Buddy.AllowsCreature() && !buddy.IsGameObject)
                        warnings.Add("This command's buddy must be a gameobject, not a creature.");
                }

                if (command.Id == RespawnCommandId && buddy.Provided && !buddy.IncludeDespawned)
                    warnings.Add("RESPAWN usually needs 'include dead/despawned' to find the object.");

                if (direction.Target == SourceTargetKind.Buddy && IsNoneOnly(command.TargetTypes))
                    warnings.Add("This command ignores its target, so targeting the buddy has no effect.");
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
