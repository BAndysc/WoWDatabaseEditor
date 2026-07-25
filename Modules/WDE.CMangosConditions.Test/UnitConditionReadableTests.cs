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

namespace WDE.CMangosConditions.Test
{
    public class UnitConditionReadableTests
    {
        private MangosUnitConditionEditService service = null!;

        [SetUp]
        public async Task Setup()
        {
            var parameterFactory = Substitute.For<IParameterFactory>();
            parameterFactory.Factory(Arg.Any<string>()).Returns(Parameter.Instance);
            var dataManager = new UnitConditionDataManager(new UnitConditionDataProvider(new TestRuntimeDataService()));
            await dataManager.Initialize();
            var clauseFactory = new UnitConditionClauseFactory(dataManager, parameterFactory);

            service = new MangosUnitConditionEditService(
                Substitute.For<IWindowManager>(),
                Substitute.For<IContainerProvider>(),
                Substitute.For<IMangosUnitConditionQueryGenerator>(),
                Substitute.For<IMySqlExecutor>(),
                Substitute.For<IMangosDatabaseProvider>(),
                clauseFactory);
        }

        private static AbstractMangosUnitConditionLine Line(uint flags, params UnitConditionClause[] clauses)
        {
            var line = new AbstractMangosUnitConditionLine { Id = -2, Flags = flags };
            for (int i = 0; i < clauses.Length; ++i)
                line.SetClause(i, clauses[i]);
            return line;
        }

        [Test]
        public void TypedClause_RendersNameOpValue()
        {
            var readable = service.BuildReadable(Line(0,
                new UnitConditionClause { Variable = 12, Op = 3, Value = 20 }));
            StringAssert.Contains("Health %", readable);
            StringAssert.Contains("<", readable);
            StringAssert.Contains("20", readable);
        }

        [Test]
        public void TwoClauses_And_Or_JoinedByFlags()
        {
            var a = new UnitConditionClause { Variable = 12, Op = 3, Value = 20 };
            var b = new UnitConditionClause { Variable = 31, Op = 1, Value = 1 };

            StringAssert.Contains(" AND ", service.BuildReadable(Line(0, a, b)));
            StringAssert.Contains(" OR ", service.BuildReadable(Line(1, a, b)));
        }

        [Test]
        public void NoClauses_AlwaysTrue()
        {
            Assert.AreEqual("always true", service.BuildReadable(Line(0)));
        }

        [Test]
        public void OpNone_RendersAsAlways()
        {
            var readable = service.BuildReadable(Line(0,
                new UnitConditionClause { Variable = 12, Op = 0, Value = 0 }));
            StringAssert.Contains("(always)", readable);
        }

        [Test]
        public void UnknownVariable_FallsBack()
        {
            var readable = service.BuildReadable(Line(0,
                new UnitConditionClause { Variable = 200, Op = 1, Value = 3 }));
            StringAssert.Contains("unknown variable 200", readable);
        }
    }
}
