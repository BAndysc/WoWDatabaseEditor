using WDE.Common.Database;

namespace WDE.DbScriptsEditor.Models
{
    // Phase-1 "raw flag combo" rendering of who acts on whom, derived from data_flags.
    // This is an approximate, read-only preview; the full structural source/target editor
    // (with a proper decompiler + validation) is phase 3.
    public static class DbScriptActorResolver
    {
        // data_flags bits. The low 3 bits (0x001 BUDDY_AS_TARGET, 0x002 REVERSE_DIRECTION,
        // 0x004 SOURCE_TARGETS_SELF) are handled together as the 8-value "combo".
        private const uint BuddyByGuid = 0x010;
        private const uint BuddyIsPet = 0x020;
        private const uint BuddyIsDespawned = 0x040;
        private const uint BuddyByPool = 0x080;
        private const uint BuddyBySpawnGroup = 0x100;
        private const uint AllEligible = 0x200;
        private const uint BuddyByGo = 0x400;
        private const uint BuddyByStringId = 0x800;

        private const uint AnyBuddyLocator =
            BuddyByGuid | BuddyIsPet | BuddyIsDespawned | BuddyByPool | BuddyBySpawnGroup | BuddyByGo | BuddyByStringId;

        // buddyName resolves a (entry, isGameObject) pair to a display name (e.g. via CreatureParameter);
        // null falls back to the raw entry number.
        public static (string source, string target) Resolve(IDbScriptLine row, DbScriptTypeInfo info,
            System.Func<long, bool, string>? buddyName = null)
        {
            var flags = row.DataFlags;
            var combo = flags & 0x7;
            var buddyProvided = row.BuddyEntry != 0 || (flags & AnyBuddyLocator) != 0;

            var origSource = info.SourceLabel;
            var origTarget = info.TargetLabel;
            var buddy = BuddyLabel(row, flags, buddyName);
            var sb = buddyProvided ? buddy : origSource;

            return combo switch
            {
                0 => (sb, origTarget),                                     // source/buddy -> target
                1 => (origSource, buddyProvided ? buddy : origSource),     // source -> buddy
                2 => (origTarget, sb),                                     // target -> source/buddy
                3 => (buddy, origSource),                                  // buddy -> source
                4 => (sb, sb),                                             // source/buddy -> source/buddy
                5 => (origSource, origSource),                             // source -> source
                6 => (origTarget, origTarget),                            // target -> target
                _ => (buddy, buddy),                                       // 7: buddy -> buddy
            };
        }

        private static string BuddyLabel(IDbScriptLine row, uint flags, System.Func<long, bool, string>? buddyName)
        {
            var isGo = (flags & BuddyByGo) != 0;
            string Name(long entry) => buddyName?.Invoke(entry, isGo) ?? entry.ToString();

            string core;
            if ((flags & BuddyByGuid) != 0)
                core = $"buddy (guid {row.SearchRadius})";
            else if ((flags & BuddyByPool) != 0)
                core = $"buddy (pool {row.SearchRadius})";
            else if ((flags & BuddyBySpawnGroup) != 0)
                core = $"buddy (spawn group {row.BuddyEntry})";
            else if ((flags & BuddyByStringId) != 0)
                core = $"buddy (string id {row.BuddyEntry})";
            else if ((flags & BuddyIsPet) != 0)
                core = $"pet of {Name(row.BuddyEntry)}";
            else if (row.BuddyEntry != 0)
                core = (isGo ? "buddy GO " : "buddy ") + Name(row.BuddyEntry);
            else
                core = "buddy";

            if ((flags & AllEligible) != 0)
                core = "all " + core + "s";
            if ((flags & BuddyIsDespawned) != 0)
                core += " (incl. dead)";
            return core;
        }
    }
}
