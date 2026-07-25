using System;
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
    public class DescriptionChooseTests
    {
        private static string Expand(string description, Dictionary<string, string> values) =>
            DbScriptDescriptionChoose.Expand(description, name => values.TryGetValue(name, out var v) ? v : null);

        [Test]
        public void Choose_PicksMatchingOption()
        {
            Assert.AreEqual("in all seats",
                Expand("{Seat:choose(-1):in all seats|in seat {Seat}}", new() { ["Seat"] = "-1" }));
        }

        [Test]
        public void Choose_FallsBackToDefaultOption_WithNestedToken()
        {
            Assert.AreEqual("in seat {Seat}",
                Expand("{Seat:choose(-1):in all seats|in seat {Seat}}", new() { ["Seat"] = "2" }));
        }

        [Test]
        public void Choose_EmptyOption_ProducesNothing()
        {
            Assert.AreEqual("x", Expand("x{Mode:choose(2):| in {Radius} yd}", new() { ["Mode"] = "2" }));
            Assert.AreEqual("x in {Radius} yd", Expand("x{Mode:choose(2):| in {Radius} yd}", new() { ["Mode"] = "1" }));
        }

        [Test]
        public void Choose_MultipleArgs_MatchByIndex()
        {
            var template = "{M:choose(1,2,3):one|two|three|other}";
            Assert.AreEqual("one", Expand(template, new() { ["M"] = "1" }));
            Assert.AreEqual("two", Expand(template, new() { ["M"] = "2" }));
            Assert.AreEqual("three", Expand(template, new() { ["M"] = "3" }));
            Assert.AreEqual("other", Expand(template, new() { ["M"] = "77" }));
        }

        [Test]
        public void Choose_NoDefaultOption_UnmatchedProducesNothing()
        {
            Assert.AreEqual("ab", Expand("a{M:choose(1):one}b", new() { ["M"] = "5" }));
        }

        [Test]
        public void PlainAndUnknownTokens_AreLeftForSecondPass()
        {
            Assert.AreEqual("{Name} and {Unknown:choose(1):a|b}",
                Expand("{Name} and {Unknown:choose(1):a|b}", new() { ["Name"] = "7" }));
        }

        [Test]
        public void Choose_Nested_Recurses()
        {
            var values = new Dictionary<string, string> { ["A"] = "1", ["B"] = "2" };
            Assert.AreEqual("inner-two",
                Expand("{A:choose(1):{B:choose(1):inner-one|inner-two}|outer-else}", values));
        }

        // End-to-end through the read-only step renderer with the real commands.json:
        // RECALL_OR_RESPAWN_ACCESSORIES (56) uses seat/mode conditionals.
        [Test]
        public async Task RecallAccessories_Readable_UsesSeatAndModeConditionals()
        {
            var factory = Substitute.For<IParameterFactory>();
            factory.Factory(Arg.Any<string>()).Returns(Parameter.Instance);
            factory.FactoryFloat(Arg.Any<string>()).Returns(FloatParameter.Instance);
            var manager = new DbScriptDataManager(new DbScriptDataProvider(new TestRuntimeDataService()), factory, Substitute.For<IMangosConditionService>(),
                Substitute.For<IMangosDatabaseProvider>(), Substitute.For<ITableEditorPickerService>());
            await manager.Initialize();
            var typeInfo = DbScriptTypes.GetInfo(DbScriptType.Relay);

            var allSeats = new AbstractDbScriptLine { Command = 56, DataLong = 1, DataLong2 = 10, DataInt = -1 };
            var readable = new DbScriptStep(allSeats, manager.GetCommand(56), typeInfo, factory).Readable;
            StringAssert.Contains("in all seats", readable);
            StringAssert.Contains("in 10 yd", readable);
            StringAssert.DoesNotContain("in seat", readable);

            var oneSeatRespawn = new AbstractDbScriptLine { Command = 56, DataLong = 2, DataLong2 = 10, DataInt = 3 };
            readable = new DbScriptStep(oneSeatRespawn, manager.GetCommand(56), typeInfo, factory).Readable;
            StringAssert.Contains("in seat 3", readable);
            StringAssert.DoesNotContain("yd", readable);
        }

        [Test]
        public async Task ApplyParameterDefaults_PrefillsSeatIndexMinusOne()
        {
            var factory = Substitute.For<IParameterFactory>();
            factory.Factory(Arg.Any<string>()).Returns(Parameter.Instance);
            factory.FactoryFloat(Arg.Any<string>()).Returns(FloatParameter.Instance);
            var manager = new DbScriptDataManager(new DbScriptDataProvider(new TestRuntimeDataService()), factory, Substitute.For<IMangosConditionService>(),
                Substitute.For<IMangosDatabaseProvider>(), Substitute.For<ITableEditorPickerService>());
            await manager.Initialize();

            var script = new EditableDbScript(DbScriptType.Relay, 1, manager, factory);
            var step = script.MakeStep(new AbstractDbScriptLine { Command = 56 });
            step.ApplyParameterDefaults();

            Assert.AreEqual(-1, step.ToLine().DataInt);
        }
    }
}
