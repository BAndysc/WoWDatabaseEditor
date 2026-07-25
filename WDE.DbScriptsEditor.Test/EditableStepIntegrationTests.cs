using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.DbScriptsEditor.Data;
using WDE.DbScriptsEditor.Models;

namespace WDE.DbScriptsEditor.Test
{
    // End-to-end over the live editable step: raw row → decode → readable, apply a leaf via the
    // compiler → ToLine, and the byte-identity guarantee for untouched legacy rows.
    public class EditableStepIntegrationTests
    {
        private DbScriptDataManager manager = null!;
        private IParameterFactory factory = null!;

        [SetUp]
        public async Task Setup()
        {
            factory = Substitute.For<IParameterFactory>();
            factory.IsRegisteredLong(Arg.Any<string>()).Returns(false);
            factory.IsRegisteredFloat(Arg.Any<string>()).Returns(false);
            factory.Factory(Arg.Any<string>()).Returns(Parameter.Instance);
            factory.Factory((string?)null).Returns(Parameter.Instance);
            manager = new DbScriptDataManager(new DbScriptDataProvider(new TestRuntimeDataService()), factory,
                Substitute.For<IMangosConditionService>(), Substitute.For<IMangosDatabaseProvider>(),
                Substitute.For<ITableEditorPickerService>());
            await manager.Initialize();
        }

        private EditableDbScriptStep Step(AbstractDbScriptLine line) =>
            new(line, manager, DbScriptTypes.GetInfo(DbScriptType.Gossip), factory);

        [Test]
        public void SpawnGroupBuddyLeaf_Applies_ToFlagsAndReadable()
        {
            // TALK (command 0), source = "all members of spawn group 5".
            var step = Step(new AbstractDbScriptLine { Id = 1, Command = 0 });
            var leaf = System.Linq.Enumerable.First(DbScriptBuddyLeaves.All, l => l.Id == "spawngroup_all");
            var updated = DbScriptSourceTargetCompiler.SetSlot(step.DecodeFlags(), isSource: true,
                SourceTargetKind.Buddy, leaf.ToDescriptor(entry: 5, searchValue: 0));
            step.ApplyDecodedFlags(updated);

            var line = step.ToLine();
            Assert.AreEqual(DbScriptFlags.BuddyBySpawnGroup | DbScriptFlags.AllEligibleBuddies,
                line.DataFlags & DbScriptFlags.BuddyRegionMask);
            Assert.AreEqual(5, line.BuddyEntry);
            // buddy is the source (sentence start), so it reads capitalized
            StringAssert.Contains("All members of spawn group", step.Readable);
        }

        [Test]
        public void UntouchedLegacyRow_WithInertBits_SerializesByteIdentical()
        {
            // A hand-written TALK row carrying 0x400 (BUDDY_BY_GO, inert on a creature command).
            var original = new AbstractDbScriptLine
            {
                Id = 7, Command = 0,
                DataFlags = DbScriptFlags.BuddyByGo, // 0x400, ignored by the core here
                BuddyEntry = 1234, SearchRadius = 8,
            };
            var step = Step(original);

            // No edit — ToLine must reproduce the raw columns exactly.
            var line = step.ToLine();
            Assert.AreEqual(original.DataFlags, line.DataFlags);
            Assert.AreEqual(original.BuddyEntry, line.BuddyEntry);
            Assert.AreEqual(original.SearchRadius, line.SearchRadius);

            // And the inert bit survives even a decode→encode round-trip via ApplyDecodedFlags.
            step.ApplyDecodedFlags(step.DecodeFlags());
            Assert.AreEqual(DbScriptFlags.BuddyByGo, step.ToLine().DataFlags & DbScriptFlags.BuddyByGo);
        }

        [Test]
        public void BuddyBothDirection_ChangeTargetToOriginal_Applies()
        {
            // flags = 7 (BUDDY_AS_TARGET | REVERSE | SOURCE_TARGETS_SELF) + buddy-by-entry => (B,B)
            var step = new EditableDbScriptStep(
                new AbstractDbScriptLine { Id = 1, Command = 0, DataFlags = 7, BuddyEntry = 100, SearchRadius = 10 },
                manager, DbScriptTypes.GetInfo(DbScriptType.QuestEnd), factory);
            var current = step.DecodeFlags();
            Assert.AreEqual(SourceTargetKind.Buddy, current.Direction.Source, "precondition: source is buddy");
            Assert.AreEqual(SourceTargetKind.Buddy, current.Direction.Target, "precondition: target is buddy");

            // user picks the original target ("Player") for the target slot
            var updated = DbScriptSourceTargetCompiler.SetSlot(current, isSource: false,
                SourceTargetKind.OriginalTarget, BuddyDescriptor.None);
            step.ApplyDecodedFlags(updated);

            var after = step.DecodeFlags();
            Assert.AreEqual(SourceTargetKind.Buddy, after.Direction.Source);
            Assert.AreEqual(SourceTargetKind.OriginalTarget, after.Direction.Target, "target should now be the original target");

            // and the readable / resolved target must reflect it (this is what the button shows)
            var (_, resolvedTarget) = step.ResolveActors();
            Assert.AreEqual(DbScriptTypes.GetInfo(DbScriptType.QuestEnd).TargetLabel, resolvedTarget,
                "resolved target label should be the original target, not the buddy");
        }

        [Test]
        public void ResolvedTarget_Updates_EvenWhenReadableIsUnchanged()
        {
            // PLAY_SOUND (16): target-capable, but its description is "{source}: Play sound {Sound}"
            // with no {target}. Starting from (source, target) with no buddy, switching the target
            // from the original target to the original source leaves the readable string identical
            // (no buddy links shift the parameter indices) — the exact case the old FormattedReadable
            // -driven button binding missed.
            var info = DbScriptTypes.GetInfo(DbScriptType.QuestEnd);
            var step = new EditableDbScriptStep(
                new AbstractDbScriptLine { Id = 1, Command = 16 }, manager, info, factory);

            var beforeReadable = step.FormattedReadable;
            var beforeTarget = step.ResolvedTarget;
            Assert.AreEqual(info.TargetLabel, beforeTarget, "precondition: target is the original target");

            var updated = DbScriptSourceTargetCompiler.SetSlot(step.DecodeFlags(), isSource: false,
                SourceTargetKind.OriginalSource, BuddyDescriptor.None);
            step.ApplyDecodedFlags(updated);

            Assert.AreEqual(beforeReadable, step.FormattedReadable, "readable is identical (no {target} rendered)");
            Assert.AreNotEqual(beforeTarget, step.ResolvedTarget, "resolved target must still update");
            Assert.AreEqual(info.SourceLabel, step.ResolvedTarget, "target now mirrors the source actor");
        }

        [Test]
        public void PlayerCommand_ResolvesToActualPlayerActor()
        {
            // Teleport (6) in a QuestStart script: source = "Quest giver" (never a player), target =
            // "Player". {player} must resolve to the real actor — the target "Player", not the source.
            var qs = new EditableDbScriptStep(new AbstractDbScriptLine { Id = 1, Command = 6 },
                manager, DbScriptTypes.GetInfo(DbScriptType.QuestStart), factory);
            var qsInfo = DbScriptTypes.GetInfo(DbScriptType.QuestStart);
            StringAssert.Contains(qsInfo.TargetLabel, qs.Readable);      // "Player"
            StringAssert.DoesNotContain(qsInfo.SourceLabel, qs.Readable); // not "Quest giver"

            // On-creature-death: source "Dying creature" (not a player), target "Killer" (a unit, can
            // be a player) → the player is the killer.
            var cd = new EditableDbScriptStep(new AbstractDbScriptLine { Id = 1, Command = 6 },
                manager, DbScriptTypes.GetInfo(DbScriptType.CreatureDeath), factory);
            var cdInfo = DbScriptTypes.GetInfo(DbScriptType.CreatureDeath);
            StringAssert.Contains(cdInfo.TargetLabel, cd.Readable);       // "Killer"
            StringAssert.DoesNotContain(cdInfo.SourceLabel, cd.Readable); // not "Dying creature"
        }

        [Test]
        public void TerminateScript_ConditionBuddy_RoundTripsAndReads()
        {
            // TERMINATE_SCRIPT (31): a dangling condition buddy (nearest creature 42 within 10 yd).
            var step = Step(new AbstractDbScriptLine { Id = 1, Command = 31 });
            var leaf = System.Linq.Enumerable.First(DbScriptBuddyLeaves.All, l => l.Id == "entry_creature_nearest_alive");
            var updated = DbScriptSourceTargetCompiler.SetConditionBuddy(step.DecodeFlags(),
                leaf.ToDescriptor(entry: 42, searchValue: 10));
            step.ApplyDecodedFlags(updated);

            var decoded = step.DecodeFlags();
            Assert.IsTrue(decoded.Buddy.Provided);
            Assert.IsFalse(decoded.Direction.UsesBuddy, "condition buddy occupies neither slot");
            StringAssert.Contains("condition:", step.Readable);
        }
    }
}
