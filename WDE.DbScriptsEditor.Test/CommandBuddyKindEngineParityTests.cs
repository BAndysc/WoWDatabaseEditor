using System.Collections.Generic;
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
    // Pins commands.json's per-command buddy kind against the core's ScriptInfo::IsCreatureBuddy /
    // IsCreatureAndGOBuddy switch. If someone edits the json (or the core changes), this fails loud.
    public class CommandBuddyKindEngineParityTests
    {
        // ScriptInfo::IsCreatureBuddy returns GO for these command ids.
        private static readonly HashSet<uint> GameObjectCommands = new() { 9, 11, 12, 13, 27, 40, 43 };
        // IsCreatureAndGOBuddy: BUDDY_BY_GO chooses creature-vs-GO for these.
        private static readonly HashSet<uint> DualCommands = new() { 31, 36, 37 };

        private DbScriptDataManager manager = null!;

        [SetUp]
        public async Task Setup()
        {
            var factory = Substitute.For<IParameterFactory>();
            factory.IsRegisteredLong(Arg.Any<string>()).Returns(false);
            factory.IsRegisteredFloat(Arg.Any<string>()).Returns(false);
            factory.Factory(Arg.Any<string>()).Returns(Parameter.Instance);
            manager = new DbScriptDataManager(new DbScriptDataProvider(new TestRuntimeDataService()), factory,
                Substitute.For<IMangosConditionService>(), Substitute.For<IMangosDatabaseProvider>(),
                Substitute.For<ITableEditorPickerService>());
            await manager.Initialize();
        }

        [Test]
        public void EveryCommandBuddyKindMatchesEngine()
        {
            for (uint id = 0; id <= 57; id++)
            {
                if (!manager.Contains(id))
                    continue;
                var expected = GameObjectCommands.Contains(id) ? DbScriptBuddyCapability.GameObject
                    : DualCommands.Contains(id) ? DbScriptBuddyCapability.Both
                    : DbScriptBuddyCapability.Creature;
                Assert.AreEqual(expected, manager.GetCommand(id).Buddy, $"command {id} buddy kind mismatch");
            }
        }
    }
}
