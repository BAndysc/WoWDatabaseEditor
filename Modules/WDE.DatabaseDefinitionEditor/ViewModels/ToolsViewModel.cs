using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using PropertyChanged.SourceGenerator;
using WDE.Common;
using WDE.Common.Managers;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Module.Attributes;

#pragma warning disable 4014

namespace WDE.DatabaseDefinitionEditor.ViewModels
{
    [AutoRegister]
    public partial class ToolsViewModel : BindableBase, IConfigurable
    {
        public ICommand Save { get; }
        public ImageUri Icon { get; } = new ImageUri("Icons/document_table1_big.png");
        public string Name => "Database table editor";
        public string ShortDescription => "This is not really settings, it is a tool to generate table definitions for new tables in your database";
        public bool IsModified => DefinitionEditor.IsModified;
        public bool IsRestartRequired => false;
        public ConfigurableGroup Group => ConfigurableGroup.Advanced;
        private bool opened;

        public DefinitionGeneratorViewModel Definitions { get; }
        public CompatibilityCheckerViewModel Compatibility { get; }
        public CoverageViewModel Coverage { get; }
        public DefinitionEditorViewModel DefinitionEditor { get; }

        [Notify] private int selectedTabIndex;

        public ToolsViewModel(DefinitionGeneratorViewModel definitionGeneratorViewModel,
            CompatibilityCheckerViewModel compatibilityCheckerViewModel,
            CoverageViewModel coverageViewModel,
            DefinitionEditorViewModel definitionEditor)
        {
            Definitions = definitionGeneratorViewModel;
            Compatibility = compatibilityCheckerViewModel;
            Coverage = coverageViewModel;
            DefinitionEditor = definitionEditor;
            Save = new AsyncAutoCommand(async () =>
            {
                if (DefinitionEditor.SelectedTable is { } table)
                    DefinitionEditor.Save(table);
            });
        }
        
        public void ConfigurableOpened()
        {
            if (!opened)
            {
                opened = true;
                Definitions.PopulateTables();
            }
        }

        public void SwitchToDefinitionEditor()
        {
            SelectedTabIndex = 3; // Index of the tab in DefinitionToolView.axaml
        }
    }
}