using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using WDE.Common.Types;
using WDE.DatabaseEditors.CustomCommands;
using WDE.MVVM;

namespace WDE.DatabaseEditors.ViewModels
{
    public class TableCommandViewModel : ObservableBase
    {
        public ImageUri Icon { get; }
        public string Name { get; }
        public ICommand Command { get; }
        
        public TableCommandViewModel(IDatabaseTableCommand command, ICommand cmd)
        {
            Icon = command.Icon;
            Name = command.Name;
            Command = cmd;
        }
        
        public TableCommandViewModel(IDatabaseTablePerKeyCommand command, ICommand cmd)
        {
            Icon = command.Icon;
            Name = command.Name;
            Command = cmd;
        }
    }
}