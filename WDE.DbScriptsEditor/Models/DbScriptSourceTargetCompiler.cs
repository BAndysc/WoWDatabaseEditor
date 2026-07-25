namespace WDE.DbScriptsEditor.Models
{
    // Context marker placed in a step's ReadableContext for the clickable source / target token.
    // Clicking it opens the high-level actor picker (which compiles down to flags + buddy).
    public sealed class DbScriptActorSlot
    {
        public bool IsSource { get; }
        public EditableDbScriptStep Step { get; }
        public DbScriptActorSlot(EditableDbScriptStep step, bool isSource)
        {
            Step = step;
            IsSource = isSource;
        }
    }

    // Context marker for the clickable "if <condition>" row.
    // Clicking it opens the cmangos condition tree editor for the if row's condition.
    public sealed class DbScriptConditionSlot
    {
        public DbScriptIfRow IfRow { get; }
        public DbScriptConditionSlot(DbScriptIfRow ifRow)
        {
            IfRow = ifRow;
        }
    }

    // Turns a high-level "set this slot to X" request into a new DecodedFlags, keeping the other
    // slot and all non-direction state. Enforces the codec invariant that a buddy is provided iff
    // one of the two slots is the buddy, so the (source,target) pair always round-trips.
    public static class DbScriptSourceTargetCompiler
    {
        public static DecodedFlags SetSlot(in DecodedFlags current, bool isSource,
            SourceTargetKind newKind, BuddyDescriptor newBuddy)
        {
            var sourceKind = isSource ? newKind : current.Direction.Source;
            var targetKind = isSource ? current.Direction.Target : newKind;
            var direction = new ScriptDirection(sourceKind, targetKind);

            BuddyDescriptor buddy;
            if (!direction.UsesBuddy)
                buddy = BuddyDescriptor.None;                 // neither slot is a buddy → no locator
            else if (newKind == SourceTargetKind.Buddy)
                buddy = newBuddy;                             // the slot we just set defines the buddy
            else
                buddy = current.Buddy.Provided ? current.Buddy : newBuddy; // other slot owns the buddy

            return new DecodedFlags(direction, current.CommandAdditional, buddy, current.UnmodeledBits);
        }
    }
}
