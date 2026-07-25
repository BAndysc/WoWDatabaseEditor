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

    // Context marker for the clickable "terminate if <buddy> found" token on TERMINATE_SCRIPT.
    // Clicking it opens the buddy leaf picker to set/clear a dangling condition buddy.
    public sealed class DbScriptBuddyConditionSlot
    {
        public EditableDbScriptStep Step { get; }
        public DbScriptBuddyConditionSlot(EditableDbScriptStep step)
        {
            Step = step;
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

            // A fresh buddy choice starts from clean canonical bits; an unchanged buddy keeps the
            // raw row's inert bits.
            var inert = buddy.Equals(current.Buddy) ? current.InertBuddyBits : 0u;
            return current.With(direction, buddy: buddy, inertBuddyBits: inert);
        }

        // Sets or clears a "condition" buddy — one that is located but occupies neither slot (the
        // core's buddyFound fallback, used by TERMINATE_SCRIPT's "terminate if found"). A dangling
        // buddy is only representable with a self direction (combos 5/6); any other combo would
        // decode the buddy back into a slot, so force source→source unless a self direction is
        // already in place.
        public static DecodedFlags SetConditionBuddy(in DecodedFlags current, BuddyDescriptor buddy)
        {
            var direction = current.Direction;
            var isSelf = direction.Source == direction.Target &&
                         direction.Source != SourceTargetKind.Buddy;
            if (buddy.Provided && !isSelf)
                direction = new ScriptDirection(SourceTargetKind.OriginalSource, SourceTargetKind.OriginalSource);
            else if (!buddy.Provided && direction.UsesBuddy)
                direction = new ScriptDirection(SourceTargetKind.OriginalSource, SourceTargetKind.OriginalTarget);

            var inert = buddy.Equals(current.Buddy) ? current.InertBuddyBits : 0u;
            return current.With(direction, buddy: buddy, inertBuddyBits: inert);
        }
    }
}
