using System.Collections.Generic;

namespace WDE.DbScriptsEditor.Models
{
    // The fully-decoded structural view of a step's data_flags + buddy_entry + search_radius.
    public readonly struct DecodedFlags
    {
        public ScriptDirection Direction { get; }
        public bool CommandAdditional { get; }     // 0x8 per-command switch
        public BuddyDescriptor Buddy { get; }
        public uint UnmodeledBits { get; }          // exotic bits outside ModeledMask, passed through

        public DecodedFlags(ScriptDirection direction, bool commandAdditional, BuddyDescriptor buddy, uint unmodeledBits)
        {
            Direction = direction;
            CommandAdditional = commandAdditional;
            Buddy = buddy;
            UnmodeledBits = unmodeledBits;
        }
    }

    // Compiler/decompiler between the raw (data_flags, buddy_entry, search_radius) triple and the
    // structural (Direction, buddy descriptor, command-additional, unmodeled bits) view. Pure logic,
    // no UI/DB dependencies, so the property tests can hammer it exhaustively.
    public static class DbScriptFlagsCodec
    {
        public static DecodedFlags Decode(uint dataFlags, long buddyEntry, long searchRadius)
        {
            var buddy = BuddyDescriptor.Decode(dataFlags, buddyEntry, searchRadius);
            var direction = DecodeDirection(dataFlags & DbScriptFlags.DirectionMask, buddy.Provided);
            var commandAdditional = (dataFlags & DbScriptFlags.CommandAdditional) != 0;
            var unmodeled = dataFlags & ~DbScriptFlags.ModeledMask;
            return new DecodedFlags(direction, commandAdditional, buddy, unmodeled);
        }

        // Rebuilds the raw triple. Returns false when the requested direction cannot be expressed
        // with the current buddy state (the UI only ever offers representable directions, so this
        // is a guard, not an expected path).
        public static bool TryEncode(in DecodedFlags decoded, out uint dataFlags, out long buddyEntry, out long searchRadius)
        {
            dataFlags = 0;
            buddyEntry = 0;
            searchRadius = 0;

            var (buddyBits, entry, radius) = decoded.Buddy.Encode();

            // A direction that acts as/on the buddy is only meaningful when a buddy is actually
            // located. The UI enforces this; guard here so an inconsistent state can't silently
            // encode to a lossy combo.
            if (decoded.Direction.UsesBuddy && !decoded.Buddy.Provided)
                return false;

            if (!TryEncodeDirection(decoded.Direction, decoded.Buddy.Provided, out var directionBits))
                return false;

            dataFlags = directionBits | buddyBits | decoded.UnmodeledBits;
            if (decoded.CommandAdditional)
                dataFlags |= DbScriptFlags.CommandAdditional;

            buddyEntry = entry;
            searchRadius = radius;
            return true;
        }

        // The 8-combo table from §1.3. "source/buddy" collapses to buddy when one is provided.
        public static ScriptDirection DecodeDirection(uint combo, bool buddyProvided)
        {
            var S = SourceTargetKind.OriginalSource;
            var T = SourceTargetKind.OriginalTarget;
            var B = SourceTargetKind.Buddy;
            return (combo & DbScriptFlags.DirectionMask) switch
            {
                0 => new ScriptDirection(buddyProvided ? B : S, T),
                1 => new ScriptDirection(S, B),
                2 => new ScriptDirection(T, buddyProvided ? B : S),
                3 => new ScriptDirection(B, S),
                4 => buddyProvided ? new ScriptDirection(B, B) : new ScriptDirection(S, S),
                5 => new ScriptDirection(S, S),
                6 => new ScriptDirection(T, T),
                _ => new ScriptDirection(B, B), // 7
            };
        }

        // Inverse of DecodeDirection. The (Source, Target) kind pair alone determines the combo
        // (all 9 pairs are representable). Canonical: never emits combo 4 (an alias of 5/7); self
        // forms use 5/6/7. buddyProvided is unused for combo selection (it only matters at decode
        // time, where the buddy descriptor keeps it consistent) but kept for a symmetric signature.
        public static bool TryEncodeDirection(ScriptDirection dir, bool buddyProvided, out uint combo)
        {
            var S = SourceTargetKind.OriginalSource;
            var T = SourceTargetKind.OriginalTarget;
            var B = SourceTargetKind.Buddy;

            combo = (dir.Source, dir.Target) switch
            {
                _ when dir.Source == S && dir.Target == T => 0u,
                _ when dir.Source == B && dir.Target == T => 0u,
                _ when dir.Source == S && dir.Target == B => 1u,
                _ when dir.Source == T && dir.Target == S => 2u,
                _ when dir.Source == T && dir.Target == B => 2u,
                _ when dir.Source == B && dir.Target == S => 3u,
                _ when dir.Source == S && dir.Target == S => 5u,
                _ when dir.Source == T && dir.Target == T => 6u,
                _ => 7u, // (B, B)
            };
            return true;
        }

        // The directions the UI may offer for the current buddy state (only representable ones).
        public static IReadOnlyList<ScriptDirection> AllowedDirections(bool buddyProvided)
        {
            var S = SourceTargetKind.OriginalSource;
            var T = SourceTargetKind.OriginalTarget;
            var B = SourceTargetKind.Buddy;
            if (!buddyProvided)
            {
                return new[]
                {
                    new ScriptDirection(S, T),
                    new ScriptDirection(T, S),
                    new ScriptDirection(S, S),
                    new ScriptDirection(T, T),
                };
            }
            return new[]
            {
                new ScriptDirection(B, T),
                new ScriptDirection(S, B),
                new ScriptDirection(T, B),
                new ScriptDirection(B, S),
                new ScriptDirection(B, B),
                new ScriptDirection(S, S),
                new ScriptDirection(T, T),
            };
        }
    }
}
