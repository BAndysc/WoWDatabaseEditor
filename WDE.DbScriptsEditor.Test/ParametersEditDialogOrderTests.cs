using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.DbScriptsEditor.Data;
using WDE.DbScriptsEditor.Editor.ViewModels.Editing;
using WDE.DbScriptsEditor.Models;
using WDE.Parameters.Models;

namespace WDE.DbScriptsEditor.Test
{
    public class ParametersEditDialogOrderTests
    {
        // Rows hide/unhide live while editing (command change remaps holders, the 0x8 switch or a
        // buddy mode unlocks rows). A row that reappears must come back at its authored position,
        // not get appended at the bottom of the dialog.
        [Test]
        public async Task RowsKeepAuthoredOrder_WhenHiddenRowReappears()
        {
            var factory = Substitute.For<IParameterFactory>();
            factory.Factory(Arg.Any<string>()).Returns(Parameter.Instance);
            factory.FactoryFloat(Arg.Any<string>()).Returns(FloatParameter.Instance);
            var manager = new DbScriptDataManager(new DbScriptDataProvider(new TestRuntimeDataService()), factory, Substitute.For<IMangosConditionService>(),
                Substitute.For<IMangosDatabaseProvider>(), Substitute.For<ITableEditorPickerService>());
            await manager.Initialize();

            var script = new EditableDbScript(DbScriptType.Relay, 1, manager, factory);
            var step = script.MakeStep(new AbstractDbScriptLine { Command = 15 });

            var a = new ParameterValueHolder<long>("A", Parameter.Instance, 0);
            var b = new ParameterValueHolder<long>("B", Parameter.Instance, 0);
            var c = new ParameterValueHolder<long>("C", Parameter.Instance, 0);

            using var vm = new DbScriptParametersEditViewModel(
                Substitute.For<IParameterPickerService>(), step, false,
                new[] { (a, "Parameters"), (b, "Parameters"), (c, "Parameters") });

            string[] Names() => vm.FilteredParameters
                .SelectMany(g => g)
                .OfType<EditableParameterViewModel<long>>()
                .Select(p => p.Parameter.Name)
                .ToArray();

            CollectionAssert.AreEqual(new[] { "A", "B", "C" }, Names());

            b.IsUsed = false; // value == 0 → row hides
            CollectionAssert.AreEqual(new[] { "A", "C" }, Names());

            b.IsUsed = true; // must return to the middle, not the bottom
            CollectionAssert.AreEqual(new[] { "A", "B", "C" }, Names());
        }
    }
}
