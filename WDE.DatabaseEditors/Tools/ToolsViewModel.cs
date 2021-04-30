using System.Windows.Input;
using Prism.Mvvm;
using WDE.Common;
using WDE.Common.Utils;
using WDE.Module.Attributes;

#pragma warning disable 4014

namespace WDE.DatabaseEditors.Tools
{
#if DEBUGAVALONIA
    [AutoRegister]
    public class ToolsViewModel : BindableBase, IConfigurable
    {
        public ICommand Save => AlwaysDisabledCommand.Command;
        public string Name => "Database table editor";
        public string ShortDescription => "This is not really settings, it is a tool to generate table definitions for new tables in your database";
        public bool IsModified => false;
        public bool IsRestartRequired => false;
        private bool opened;

        public DefinitionGeneratorViewModel Definitions { get; }
        public CompatibilityCheckerViewModel Compatibility { get; }
        
        public ToolsViewModel(DefinitionGeneratorViewModel definitionGeneratorViewModel,
            CompatibilityCheckerViewModel compatibilityCheckerViewModel)
        {
            Definitions = definitionGeneratorViewModel;
            Compatibility = compatibilityCheckerViewModel;
        }
        
        public void ConfigurableOpened()
        {
            if (!opened)
            {
                opened = true;
                Definitions.PopulateTables();
            }
        }
    }
#endif
}