using NUnit.Framework;
using WDE.DbScriptsEditor.Models;

namespace WDE.DbScriptsEditor.Test
{
    // Proves DbScriptFlagsCodec.DecodeDirection reproduces the core's actual source/target pipeline
    // (the tail of ScriptAction::GetScriptProcessTargets + HandleScriptStep): buddy replaces source
    // (default) or target (BUDDY_AS_TARGET), then optional swap (REVERSE_DIRECTION), then optional
    // target := source (SOURCE_TARGETS_SELF). The oracle below is a symbolic re-implementation over
    // single-element sets {S}/{T}/{B}.
    public class EngineOracleDirectionTests
    {
        // A symbolic slot: which original object occupies it, or Empty when the core would leave it
        // with no object (a degenerate data row).
        private enum Slot { Empty, S, T, B }

        // The engine's flag pipeline over symbolic single-element sets.
        private static (Slot source, Slot target) EngineResolve(uint combo, bool buddyProvided)
        {
            var fs = Slot.S;
            var ft = Slot.T;
            var buddy = buddyProvided ? Slot.B : Slot.Empty;

            if ((combo & DbScriptFlags.BuddyAsTarget) != 0)
                ft = buddy;                       // finalTargets = buddies
            else if (buddyProvided)
                fs = buddy;                       // if (!buddies.empty()) finalSources = buddies

            if ((combo & DbScriptFlags.ReverseDirection) != 0)
                (fs, ft) = (ft, fs);              // swap

            if ((combo & DbScriptFlags.SourceTargetsSelf) != 0)
                ft = fs;                          // finalTargets = finalSources

            return (fs, ft);
        }

        private static Slot ToSlot(SourceTargetKind kind) => kind switch
        {
            SourceTargetKind.OriginalSource => Slot.S,
            SourceTargetKind.OriginalTarget => Slot.T,
            _ => Slot.B,
        };

        [Test]
        public void WithBuddy_AllEightCombosMatchEngine()
        {
            for (uint combo = 0; combo < 8; combo++)
            {
                var (engineS, engineT) = EngineResolve(combo, buddyProvided: true);
                var dir = DbScriptFlagsCodec.DecodeDirection(combo, buddyProvided: true);
                Assert.AreEqual(engineS, ToSlot(dir.Source), $"combo {combo}: source mismatch");
                Assert.AreEqual(engineT, ToSlot(dir.Target), $"combo {combo}: target mismatch");
            }
        }

        [Test]
        public void WithoutBuddy_NonBuddyCombosMatchEngine()
        {
            // Combos that never route through the (absent) buddy: their result is fully determined.
            foreach (var combo in new uint[] { 0, 2, 4, 5, 6 })
            {
                var (engineS, engineT) = EngineResolve(combo, buddyProvided: false);
                var dir = DbScriptFlagsCodec.DecodeDirection(combo, buddyProvided: false);
                Assert.AreEqual(engineS, ToSlot(dir.Source), $"combo {combo}: source mismatch");
                Assert.AreEqual(engineT, ToSlot(dir.Target), $"combo {combo}: target mismatch");
            }
        }

        [Test]
        public void WithoutBuddy_BuddyRoutingCombosAreDegenerate()
        {
            // Combos 1/3/7 route a slot through the buddy set; with no buddy the core leaves that
            // slot empty. The codec has no "empty slot" kind, so it reports a Buddy reference — an
            // un-encodable degenerate state (guarded by TryEncode). Document that here.
            foreach (var combo in new uint[] { 1, 3, 7 })
            {
                var (engineS, engineT) = EngineResolve(combo, buddyProvided: false);
                Assert.IsTrue(engineS == Slot.Empty || engineT == Slot.Empty,
                    $"combo {combo}: expected an empty slot in the engine result");
                var dir = DbScriptFlagsCodec.DecodeDirection(combo, buddyProvided: false);
                Assert.IsTrue(dir.UsesBuddy, $"combo {combo}: codec should mark the degenerate buddy reference");
            }
        }

        [Test]
        public void DecodeEncodeDirection_RoundTripsForRepresentablePairs()
        {
            // Every (source,target) pair the codec can produce must encode back to a combo that
            // decodes to the same pair.
            for (uint combo = 0; combo < 8; combo++)
            {
                var dir = DbScriptFlagsCodec.DecodeDirection(combo, buddyProvided: true);
                Assert.IsTrue(DbScriptFlagsCodec.TryEncodeDirection(dir, true, out var combo2));
                var dir2 = DbScriptFlagsCodec.DecodeDirection(combo2, buddyProvided: true);
                Assert.AreEqual(dir, dir2, $"combo {combo} did not round-trip");
            }
        }
    }
}
