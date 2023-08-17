using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common;
using WDE.Common.Managers;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Module.Attributes;

#pragma warning disable 4014

namespace WDE.DatabaseDefinitionEditor.ViewModels
{
    [AutoRegister]
    public class ToolsViewModel : BindableBase, IConfigurable, IWindowViewModel
    {
        public ICommand Save { get; }
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

        public int DesiredWidth => 1024;
        public int DesiredHeight => 768;
        public string Title => "Table definitions editor";
        public bool Resizeable => true;
        public ImageUri? Icon => new ImageUri("Icons/icon_edit.png");
    }
}