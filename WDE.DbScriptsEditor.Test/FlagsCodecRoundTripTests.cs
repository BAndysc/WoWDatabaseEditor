using System;
using System.Collections.Generic;
using NUnit.Framework;
using WDE.DbScriptsEditor.Models;

namespace WDE.DbScriptsEditor.Test
{
    // Exhaustive round-trip guarantees for the data_flags/buddy_entry/search_radius codec. These
    // are the safety net for the "legacy rows keep their exact data_flags" requirement: a decode
    // followed by an encode must reproduce the raw bits, save for one documented canonicalization.
    public class FlagsCodecRoundTripTests
    {
        private static readonly DbScriptBuddyCapability[] Kinds =
        {
            DbScriptBuddyCapability.Creature,
            DbScriptBuddyCapability.GameObject,
            DbScriptBuddyCapability.Both,
        };

        // (entry, radius) pairs that exercise both "buddy located" and "no buddy" states, plus a
        // dangling search_radius with no entry.
        private static readonly (long entry, long radius)[] Columns =
        {
            (0, 0), (1234, 10), (0, 5), (1234, 0),
        };

        [Test]
        public void Decode_ThenEncode_ReproducesRawBits_ForAllModeledFlags()
        {
            for (uint flags = 0; flags <= DbScriptFlags.ModeledMask; flags++)
            {
                foreach (var kind in Kinds)
                foreach (var (entry, radius) in Columns)
                {
                    var decoded = DbScriptFlagsCodec.Decode(flags, entry, radius, kind);
                    var ok = DbScriptFlagsCodec.TryEncode(decoded, out var flags2, out var entry2, out var radius2);

                    // The only non-encodable states are degenerate raw rows: a direction that acts
                    // as/on the buddy while no buddy is actually located (BUDDY_AS_TARGET/reverse bit
                    // with buddy_entry == 0). Those are data errors with no editor representation.
                    var degenerate = decoded.Direction.UsesBuddy && !decoded.Buddy.Provided;
                    Assert.AreEqual(!degenerate, ok,
                        $"flags=0x{flags:X3} kind={kind} entry={entry} radius={radius}: encodable mismatch");
                    if (!ok)
                        continue;

                    // Buddy region (0xFF0), the command-additional bit and any unmodeled bits must be
                    // bit-identical. Only the low 3 direction bits may canonicalize (combo 4 with a
                    // buddy is an alias of 7; combo 5 without a buddy is a load-invalid row the
                    // encoder repairs to 4).
                    Assert.AreEqual(flags & DbScriptFlags.BuddyRegionMask, flags2 & DbScriptFlags.BuddyRegionMask,
                        $"flags=0x{flags:X3} kind={kind}: buddy region not preserved (got 0x{flags2:X3})");
                    Assert.AreEqual(flags & DbScriptFlags.CommandAdditional, flags2 & DbScriptFlags.CommandAdditional,
                        $"flags=0x{flags:X3} kind={kind}: command-additional bit not preserved");
                    Assert.AreEqual(flags & ~DbScriptFlags.ModeledMask, flags2 & ~DbScriptFlags.ModeledMask,
                        $"flags=0x{flags:X3} kind={kind}: unmodeled bits not preserved");

                    var combo = flags & DbScriptFlags.DirectionMask;
                    var combo2 = flags2 & DbScriptFlags.DirectionMask;
                    if (combo == 4)
                        Assert.AreEqual(decoded.Buddy.Provided ? 7u : 4u, combo2,
                            $"flags=0x{flags:X3}: combo 4 canonicalization wrong");
                    else if (combo == 5 && !decoded.Buddy.Provided)
                        // BUDDY_AS_TARGET without a buddy is rejected by LoadScripts; the encoder
                        // repairs the row to the load-valid self form.
                        Assert.AreEqual(4u, combo2,
                            $"flags=0x{flags:X3}: buddy-less combo 5 must be repaired to 4, got {combo2}");
                    else
                        Assert.AreEqual(combo, combo2, $"flags=0x{flags:X3} kind={kind}: direction combo changed");

                    // Columns: preserved when a buddy is located; zeroed when it isn't.
                    if (decoded.Buddy.Provided)
                    {
                        Assert.AreEqual(entry, entry2, $"flags=0x{flags:X3} kind={kind}: buddy_entry lost");
                        Assert.AreEqual(radius, radius2, $"flags=0x{flags:X3} kind={kind}: search_radius lost");
                    }
                    else
                    {
                        Assert.AreEqual(0, entry2);
                        Assert.AreEqual(0, radius2);
                    }
                }
            }
        }

        [Test]
        public void ReEncode_IsIdempotentAndStructurallyStable()
        {
            for (uint flags = 0; flags <= DbScriptFlags.ModeledMask; flags++)
            {
                foreach (var kind in Kinds)
                foreach (var (entry, radius) in Columns)
                {
                    var d1 = DbScriptFlagsCodec.Decode(flags, entry, radius, kind);
                    if (!DbScriptFlagsCodec.TryEncode(d1, out var f2, out var e2, out var r2))
                        continue;

                    var d2 = DbScriptFlagsCodec.Decode(f2, e2, r2, kind);
                    // Second decode must be structurally identical (combo-4 already canonicalized).
                    Assert.AreEqual(d1.Direction, d2.Direction, $"flags=0x{flags:X3}: direction drift");
                    Assert.AreEqual(d1.Buddy, d2.Buddy, $"flags=0x{flags:X3}: buddy drift");
                    Assert.AreEqual(d1.CommandAdditional, d2.CommandAdditional);
                    Assert.AreEqual(d1.InertBuddyBits, d2.InertBuddyBits, $"flags=0x{flags:X3}: inert-bit drift");

                    // Re-encoding the second decode reproduces the same bytes (fixed point).
                    Assert.IsTrue(DbScriptFlagsCodec.TryEncode(d2, out var f3, out var e3, out var r3));
                    Assert.AreEqual(f2, f3, $"flags=0x{flags:X3}: not a fixed point");
                    Assert.AreEqual(e2, e3);
                    Assert.AreEqual(r2, r3);
                }
            }
        }

        [Test]
        public void NoBuddyDirections_NeverSetBuddyAsTarget()
        {
            // LoadScripts skips any row with SCRIPT_FLAG_BUDDY_AS_TARGET (0x1) and no buddy
            // locator, so every buddy-less direction must encode without that bit.
            foreach (var dir in DbScriptFlagsCodec.AllowedDirections(buddyProvided: false))
            {
                Assert.IsTrue(DbScriptFlagsCodec.TryEncodeDirection(dir, false, out var combo));
                Assert.AreEqual(0u, combo & DbScriptFlags.BuddyAsTarget,
                    $"({dir.Source},{dir.Target}): buddy-less direction must not set BUDDY_AS_TARGET");
            }
        }

        [Test]
        public void InertBits_CarryFlagsTheCoreIgnores()
        {
            // 0x400 (BUDDY_BY_GO) on a creature-only command: the descriptor is a creature, but the
            // bit must still round-trip so the raw row is byte-identical.
            var decoded = DbScriptFlagsCodec.Decode(
                DbScriptFlags.BuddyByGo | 0x1 /* by-entry via entry */, buddyEntry: 55, searchRadius: 8,
                DbScriptBuddyCapability.Creature);
            Assert.IsFalse(decoded.Buddy.IsGameObject, "creature-only command must ignore BUDDY_BY_GO for kind");
            Assert.AreEqual(DbScriptFlags.BuddyByGo, decoded.InertBuddyBits & DbScriptFlags.BuddyByGo);

            Assert.IsTrue(DbScriptFlagsCodec.TryEncode(decoded, out var flags2, out _, out _));
            Assert.AreEqual(DbScriptFlags.BuddyByGo, flags2 & DbScriptFlags.BuddyByGo,
                "the ignored 0x400 bit must survive re-encoding");
        }

        [Test]
        public void UnmodeledHighBits_PassThrough()
        {
            const uint exotic = 0x8000_0000 | 0x1_0000;
            var decoded = DbScriptFlagsCodec.Decode(exotic | 0x040, buddyEntry: 7, searchRadius: 3,
                DbScriptBuddyCapability.Creature);
            Assert.AreEqual(exotic, decoded.UnmodeledBits);
            Assert.IsTrue(DbScriptFlagsCodec.TryEncode(decoded, out var flags2, out _, out _));
            Assert.AreEqual(exotic, flags2 & ~DbScriptFlags.ModeledMask);
        }
    }
}
