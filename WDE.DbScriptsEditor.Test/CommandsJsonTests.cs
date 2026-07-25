using System.Linq;
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
    public class CommandsJsonTests
    {
        private DbScriptDataManager manager = null!;

        [SetUp]
        public async Task Setup()
        {
            var factory = Substitute.For<IParameterFactory>();
            factory.IsRegisteredLong(Arg.Any<string>()).Returns(false);
            factory.IsRegisteredFloat(Arg.Any<string>()).Returns(false);
            factory.Factory(Arg.Any<string>()).Returns(Parameter.Instance);
            manager = new DbScriptDataManager(new DbScriptDataProvider(new TestRuntimeDataService()), factory, Substitute.For<IMangosConditionService>(),
                Substitute.For<IMangosDatabaseProvider>(), Substitute.For<ITableEditorPickerService>());
            await manager.Initialize();
        }

        [Test]
        public void CommandsJson_ValidatesWithZeroWarnings()
        {
            CollectionAssert.IsEmpty(manager.ValidationWarnings,
                "commands.json produced validation warnings:\n" + string.Join("\n", manager.ValidationWarnings));
        }

        [Test]
        public void AllFiftyEightCommandsLoad()
        {
            Assert.AreEqual(58, manager.AllCommands.Count);
            for (uint i = 0; i <= 57; i++)
                Assert.IsTrue(manager.Contains(i), $"command {i} missing");
        }

        [Test]
        public void ValueZeroVariants_ReadContextually()
        {
            // faction 0 => reset (engine ClearTemporaryFaction), morph/mount 0 => demorph/dismount,
            // kill-credit 0 => dynamic. The readable must reflect the value, not say "… 0".
            void Check(uint command, long datalong, string expectContains)
            {
                var (_, description, variant) = manager.GetCommand(command).Resolve(new FakeLine { Command = command, DataLong = (uint)datalong });
                Assert.IsNotNull(variant, $"command {command} datalong={datalong} should resolve a variant");
                StringAssert.Contains(expectContains, description);
            }
            Check(22, 0, "Reset faction");
            Check(23, 0, "Demorph");
            Check(24, 0, "Dismount");
            Check(8, 0, "{creature}"); // involved-creature variant resolves the source-or-target creature

            var (_, faction5, v5) = manager.GetCommand(22).Resolve(new FakeLine { Command = 22, DataLong = 5 });
            Assert.IsNull(v5, "nonzero faction should be the base 'Set faction' reading");
            StringAssert.Contains("Set faction", faction5);

            // morph entry 0 wins over the 0x8 display-id variant (engine checks entry==0 first)
            var (_, demorph, _) = manager.GetCommand(23).Resolve(new FakeLine { Command = 23, DataLong = 0, DataFlags = 0x8 });
            StringAssert.Contains("Demorph", demorph);
        }

        [Test]
        public void LoneTargetCommands_AreTargetOnly()
        {
            // drives the editor's "present lone target as source" swap (settings-gated)
            foreach (var id in new uint[] { 40, 41, 43, 52 })
            {
                var def = manager.GetCommand(id);
                Assert.IsTrue(def.EffectiveUsesTarget(null), $"command {id} should use a target");
                Assert.IsFalse(def.EffectiveUsesSource(null), $"command {id} should not use a source");
            }
            Assert.IsTrue(manager.GetCommand(13).EffectiveUsesSource(null), "ACTIVATE_OBJECT uses both");
            Assert.IsTrue(manager.GetCommand(0).EffectiveUsesSource(null), "TALK uses a source");
        }

        [Test]
        public void AcceptsNoSource_MatchesDeclaredTypes()
        {
            // drives the wizard's "(none)" source filter
            Assert.IsTrue(manager.GetCommand(9).AcceptsNoSource,  "RESPAWN_GO declares ['GameObject','None']");
            Assert.IsTrue(manager.GetCommand(31).AcceptsNoSource, "TERMINATE declares ['WorldObject','None']");
            Assert.IsTrue(manager.GetCommand(53).AcceptsNoSource, "SET_WORLDSTATE declares ['None']");
            Assert.IsFalse(manager.GetCommand(22).AcceptsNoSource, "SET_FACTION requires a creature source");
            Assert.IsFalse(manager.GetCommand(0).AcceptsNoSource,  "TALK requires a source");
        }

        [Test]
        public void Movement_TargetOnlyForWaypointAndSplineVariants()
        {
            var def = manager.GetCommand(20);
            Assert.IsFalse(def.EffectiveUsesTarget(null), "base movement should not use a target");

            (string variant, uint datalong, bool usesTarget)[] cases =
            {
                ("Movement: Idle", 0, false),
                ("Movement: Random", 1, false),
                ("Movement: Waypoint", 2, true),
                ("Movement: Spline path", 3, true),
                ("Movement: Linear waypoint", 4, false),
                ("Movement: Jump", 15, false),
                ("Movement: Fall", 18, false),
            };
            foreach (var (name, datalong, usesTarget) in cases)
            {
                var (_, _, variant) = def.Resolve(new FakeLine { Command = 20, DataLong = datalong });
                Assert.IsNotNull(variant, $"variant for datalong {datalong} not resolved");
                Assert.AreEqual(name, variant!.NameReadable);
                Assert.AreEqual(usesTarget, def.EffectiveUsesTarget(variant), $"{name} target usage");
            }
        }

        [Test]
        public void MoveTo_AdditionalFlag_ResolvesTeleportVariant()
        {
            var def = manager.GetCommand(3);
            var (_, _, variant) = def.Resolve(new FakeLine { Command = 3, DataFlags = 0x008 });
            Assert.IsNotNull(variant);
            Assert.AreEqual("Teleport to position", variant!.NameReadable);
        }

        [Test]
        public void Movement_Waypoint_ResolvesByDatalong()
        {
            var def = manager.GetCommand(20);
            var (_, _, variant) = def.Resolve(new FakeLine { Command = 20, DataLong = 2 });
            Assert.IsNotNull(variant);
            Assert.AreEqual("Movement: Waypoint", variant!.NameReadable);
        }

        [Test]
        public void Terminate_FoundVariant_ResolvesByFlag()
        {
            var def = manager.GetCommand(31);
            var (_, _, variant) = def.Resolve(new FakeLine { Command = 31, DataFlags = 0x008 });
            Assert.IsNotNull(variant);
            Assert.AreEqual("Terminate script if creature found", variant!.NameReadable);
        }

        [Test]
        public void BaseCommand_NoFlags_ResolvesNoVariant()
        {
            var def = manager.GetCommand(15);
            var (_, _, variant) = def.Resolve(new FakeLine { Command = 15, DataLong = 100 });
            Assert.IsNull(variant);
        }

        private class FakeLine : IDbScriptLine
        {
            public uint Id { get; init; }
            public uint Delay { get; init; }
            public uint Priority { get; init; }
            public uint Command { get; init; }
            public uint DataLong { get; init; }
            public uint DataLong2 { get; init; }
            public uint DataLong3 { get; init; }
            public uint BuddyEntry { get; init; }
            public uint SearchRadius { get; init; }
            public uint DataFlags { get; init; }
            public int DataInt { get; init; }
            public int DataInt2 { get; init; }
            public int DataInt3 { get; init; }
            public int DataInt4 { get; init; }
            public float DataFloat { get; init; }
            public float X { get; init; }
            public float Y { get; init; }
            public float Z { get; init; }
            public float O { get; init; }
            public float Speed { get; init; }
            public uint ConditionId { get; init; }
            public string? Comments { get; init; }
        }
    }
}
