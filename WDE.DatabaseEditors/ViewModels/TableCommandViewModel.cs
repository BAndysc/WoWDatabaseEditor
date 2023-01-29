using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using WDE.Common.Types;
using WDE.DatabaseEditors.CustomCommands;
using WDE.DatabaseEditors.Data.Structs;
using WDE.MVVM;

namespace WDE.DatabaseEditors.ViewModels
{
    public class TableCommandViewModel : ObservableBase
    {
        public ImageUri Icon { get; }
        public string Name { get; }
        public ICommand Command { get; }
        /// <summary>
        /// only for visual purposes here
        /// </summary>
        public string? KeyGesture { get; }
        
        public TableCommandViewModel(DatabaseCommandDefinitionJson definition, IDatabaseTableCommand command, ICommand cmd)
        {
            Icon = command.Icon;
            Name = command.Name;
            Command = cmd;
            KeyGesture = definition.KeyBinding;
        }
        
        public TableCommandViewModel(DatabaseCommandDefinitionJson definition, IDatabaseTablePerKeyCommand command, ICommand cmd)
        {
            Icon = command.Icon;
            Name = command.Name;
            Command = cmd;
            KeyGesture = definition.KeyBinding;
        }
    }
}