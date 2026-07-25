namespace WDE.DbScriptsEditor.Models
{
    // data_flags bits (CMaNGOS ScriptInfoDataFlags in ScriptMgr.h).
    public static class DbScriptFlags
    {
        // Low 3 bits: the direction "combo" (who acts on whom). Handled as one 0..7 value.
        public const uint BuddyAsTarget = 0x001;
        public const uint ReverseDirection = 0x002;
        public const uint SourceTargetsSelf = 0x004;

        // Per-command switch; not part of source/target/buddy. Modeled as its own toggle.
        public const uint CommandAdditional = 0x008;

        // Buddy locator / behaviour bits.
        public const uint BuddyByGuid = 0x010;
        public const uint BuddyIsPet = 0x020;
        public const uint BuddyIsDespawned = 0x040;
        public const uint BuddyByPool = 0x080;
        public const uint BuddyBySpawnGroup = 0x100; // NYI in core
        public const uint AllEligibleBuddies = 0x200;
        public const uint BuddyByGo = 0x400;
        public const uint BuddyByStringId = 0x800;

        // Bits the structural editor understands. Anything outside this mask is an unmodeled
        // exotic bit that must pass through untouched so the row still roundtrips byte-identically.
        public const uint ModeledMask = 0xFFF;

        public const uint DirectionMask = 0x7;

        // Any bit that means "a buddy is being located" (independent of buddy_entry != 0).
        public const uint AnyBuddyLocator =
            BuddyByGuid | BuddyIsPet | BuddyIsDespawned | BuddyByPool |
            BuddyBySpawnGroup | BuddyByGo | BuddyByStringId;
    }
}
