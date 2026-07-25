using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using WDE.CMangosConditions.Data;
using WDE.CMangosConditions.ViewModels;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.History;

namespace WDE.CMangosConditions.Test
{
    public class UnitConditionEditorViewModelTests
    {
        private UnitConditionDataManager dataManager = null!;
        private UnitConditionClauseFactory clauseFactory = null!;

        [SetUp]
        public async Task Setup()
        {
            var parameterFactory = Substitute.For<IParameterFactory>();
            parameterFactory.Factory(Arg.Any<string>()).Returns(Parameter.Instance);
            dataManager = new UnitConditionDataManager(new UnitConditionDataProvider(new TestRuntimeDataService()));
            await dataManager.Initialize();
            clauseFactory = new UnitConditionClauseFactory(dataManager, parameterFactory);
        }

        private UnitConditionEditorViewModel CreateViewModel(IMangosUnitConditionLine line) =>
            new(dataManager, clauseFactory, Substitute.For<IParameterPickerService>(),
                new HistoryManager(), line);

        [Test]
        public void Load_CompactsNoneGaps_Serialize_ZeroFillsTail()
        {
            var line = new AbstractMangosUnitConditionLine { Id = -2, Flags = 0 };
            line.SetClause(0, new UnitConditionClause { Variable = 12, Op = 3, Value = 20 });
            // slot 1 left empty on purpose
            line.SetClause(2, new UnitConditionClause { Variable = 76, Op = 1, Value = 12345 });

            using var vm = CreateViewModel(line);
            Assert.AreEqual(2, vm.Clauses.Count);

            var result = vm.ToLine();
            Assert.AreEqual(-2, result.Id);
            Assert.AreEqual(12u, result.GetClause(0).Variable);
            Assert.AreEqual(76u, result.GetClause(1).Variable);
            Assert.AreEqual(12345, result.GetClause(1).Value);
            for (int i = 2; i < IMangosUnitConditionLine.ClausesCount; ++i)
            {
                Assert.AreEqual(0u, result.GetClause(i).Variable);
                Assert.AreEqual(0u, result.GetClause(i).Op);
                Assert.AreEqual(0, result.GetClause(i).Value);
            }
        }

        [Test]
        public void UnknownFlagBits_ArePreserved_OrBitFollowsLogic()
        {
            var line = new AbstractMangosUnitConditionLine { Id = -2, Flags = 4 | 1 };
            using var vm = CreateViewModel(line);
            Assert.AreEqual(1, vm.IsOr.Value);

            vm.IsOr.Value = 0;
            Assert.AreEqual(4u, vm.ToLine().Flags);

            vm.IsOr.Value = 1;
            Assert.AreEqual(5u, vm.ToLine().Flags);
        }

        [Test]
        public void AddClause_BlockedAtEight()
        {
            using var vm = CreateViewModel(new AbstractMangosUnitConditionLine { Id = -2 });
            while (vm.AddClauseCommand.CanExecute())
                vm.AddClauseCommand.Execute();
            Assert.AreEqual(IMangosUnitConditionLine.ClausesCount, vm.Clauses.Count);
        }

        [Test]
        public void Validate_Blocks_Id_Zero_And_MinusOne()
        {
            using var vm = CreateViewModel(new AbstractMangosUnitConditionLine { Id = -2 });

            vm.Id.Value = 0;
            Assert.IsNotEmpty(vm.Validate());

            vm.Id.Value = -1;
            Assert.IsNotEmpty(vm.Validate());

            vm.Id.Value = -2;
            Assert.IsEmpty(vm.Validate());
        }

        [Test]
        public void UndoRedo_ValueChange()
        {
            var line = new AbstractMangosUnitConditionLine { Id = -2 };
            line.SetClause(0, new UnitConditionClause { Variable = 12, Op = 3, Value = 20 });
            using var vm = CreateViewModel(line);

            vm.Clauses[0].Value.Value = 55;
            vm.HistoryManager.Undo();
            Assert.AreEqual(20, vm.Clauses[0].Value.Value);
            vm.HistoryManager.Redo();
            Assert.AreEqual(55, vm.Clauses[0].Value.Value);
        }

        [Test]
        public void UndoRedo_VariableChange()
        {
            var line = new AbstractMangosUnitConditionLine { Id = -2 };
            line.SetClause(0, new UnitConditionClause { Variable = 12, Op = 3, Value = 20 });
            using var vm = CreateViewModel(line);

            vm.Clauses[0].SelectedVariable = dataManager.TryGetVariable(74);
            Assert.AreEqual(74, vm.Clauses[0].VariableId);

            vm.HistoryManager.Undo();
            Assert.AreEqual(12, vm.Clauses[0].VariableId);
            vm.HistoryManager.Redo();
            Assert.AreEqual(74, vm.Clauses[0].VariableId);
        }

        [Test]
        public void UndoRedo_AddRemoveClause()
        {
            using var vm = CreateViewModel(new AbstractMangosUnitConditionLine { Id = -2 });

            vm.AddClauseCommand.Execute();
            Assert.AreEqual(1, vm.Clauses.Count);

            vm.HistoryManager.Undo();
            Assert.AreEqual(0, vm.Clauses.Count);
            vm.HistoryManager.Redo();
            Assert.AreEqual(1, vm.Clauses.Count);

            var clause = vm.Clauses.Single();
            vm.RemoveClauseCommand.Execute(clause);
            Assert.AreEqual(0, vm.Clauses.Count);
            vm.HistoryManager.Undo();
            Assert.AreEqual(1, vm.Clauses.Count);
        }
    }
}
