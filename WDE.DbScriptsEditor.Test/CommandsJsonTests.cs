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
