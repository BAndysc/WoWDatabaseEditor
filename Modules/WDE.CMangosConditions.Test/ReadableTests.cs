using System.Collections.Generic;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using Prism.Ioc;
using WDE.CMangosConditions.Data;
using WDE.CMangosConditions.Services;
using WDE.CMangosConditions.ViewModels;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Common.Services.IdGenerator;

namespace WDE.CMangosConditions.Test
{
    public class ReadableTests
    {
        private MangosConditionEditService service = null!;

        [SetUp]
        public async Task Setup()
        {
            var parameterFactory = Substitute.For<IParameterFactory>();
            parameterFactory.Factory(Arg.Any<string>()).Returns(Parameter.Instance);
            var dataManager = new MangosConditionDataManager(new MangosConditionDataProvider(new TestRuntimeDataService()));
            await dataManager.Initialize();
            var factory = new MangosConditionsFactory(dataManager, parameterFactory);

            var containerProvider = Substitute.For<IContainerProvider>();
            containerProvider.Resolve(typeof(IMangosConditionsFactory)).Returns(factory);

            service = new MangosConditionEditService(
                Substitute.For<IWindowManager>(),
                containerProvider,
                Substitute.For<IMangosConditionQueryGenerator>(),
                Substitute.For<IMySqlExecutor>(),
                Substitute.For<IMangosDatabaseProvider>(),
                Substitute.For<IIdGeneratorService>());
        }

        private static AbstractMangosConditionLine Line(uint entry, int type, uint v1 = 0, uint v2 = 0, uint flags = 0) =>
            new() { ConditionEntry = entry, ConditionType = type, Value1 = v1, Value2 = v2, Flags = flags };

        [Test]
        public void Leaf_RendersDescription()
        {
            var lines = new List<IMangosConditionLine> { Line(1, 8, 123) };
            var readable = service.BuildReadable(1, lines);
            StringAssert.Contains("quest", readable);
            StringAssert.Contains("123", readable);
            StringAssert.DoesNotContain("[p]", readable);
            StringAssert.DoesNotContain("#1", readable);
        }

        [Test]
        public void LogicalTree_RendersAndOrNot()
        {
            var lines = new List<IMangosConditionLine>
            {
                Line(1, 8, 100),
                Line(2, 8, 200),
                Line(3, -2, 1, 2),  // OR(1,2)
                Line(4, 0),         // Always
                Line(5, -3, 4),     // NOT(4)
                Line(6, -1, 3, 5),  // AND(OR(1,2), NOT(4))
            };
            var readable = service.BuildReadable(6, lines);
            StringAssert.Contains(" AND ", readable);
            StringAssert.Contains(" OR ", readable);
            StringAssert.Contains("NOT (", readable);
        }

        [Test]
        public void UnknownRoot_FallsBackToId()
        {
            Assert.AreEqual("condition 77", service.BuildReadable(77, new List<IMangosConditionLine>()));
            Assert.AreEqual("", service.BuildReadable(0, new List<IMangosConditionLine>()));
        }

        [Test]
        public void NoContext_UsesTagFallbackActor()
        {
            // CONDITION_ITEM (2) is player-tagged
            var lines = new List<IMangosConditionLine> { Line(1, 2, 123, 5) };
            StringAssert.StartsWith("player ", service.BuildReadable(1, lines));
        }

        [Test]
        public void Context_SubstitutesTargetName()
        {
            var lines = new List<IMangosConditionLine> { Line(1, 2, 123, 5) };
            var readable = service.BuildReadable(1, lines, new MangosConditionSourceTarget("Player", "the vendor NPC"));
            StringAssert.StartsWith("Player ", readable);
        }

        [Test]
        public void SwappedContext_SubstitutesSourceName()
        {
            // flag 0x2 = swap source and target: the condition is checked on the source object
            var lines = new List<IMangosConditionLine> { Line(1, 1, 555, flags: 2) };
            var readable = service.BuildReadable(1, lines, new MangosConditionSourceTarget("Player", "buddy Guard"));
            StringAssert.StartsWith("buddy Guard ", readable);
            StringAssert.DoesNotContain("(source↔target)", readable);
        }

        [Test]
        public void SwappedWithoutContext_SaysSource()
        {
            var lines = new List<IMangosConditionLine> { Line(1, 1, 555, flags: 2) };
            StringAssert.StartsWith("source ", service.BuildReadable(1, lines));
        }

        [Test]
        public void SwappedContextWithoutSource_SaysNothing()
        {
            // e.g. quest_template: the core passes no source object at all
            var lines = new List<IMangosConditionLine> { Line(1, 1, 555, flags: 2) };
            var readable = service.BuildReadable(1, lines, new MangosConditionSourceTarget("Player", null));
            StringAssert.StartsWith("(nothing) ", readable);
        }

        [Test]
        public void ActorlessTemplate_KeepsSwapMarker()
        {
            // CONDITION_ACTIVE_GAME_EVENT (12) names no actor: keep the explicit swap marker
            var lines = new List<IMangosConditionLine> { Line(1, 12, 40, flags: 2) };
            StringAssert.Contains("(source↔target)", service.BuildReadable(1, lines));
        }
    }
}
