using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace WoWDatabaseEditorCore.ViewModels
{
    public class MenuItemViewModel
    {
        public MenuItemViewModel(Action action, string header)
        {
            Command = new CommandViewModel(action);
            Header = header;
            MenuItems = new ObservableCollection<MenuItemViewModel>();
        }

        public string Header { get; set; }

        public ObservableCollection<MenuItemViewModel> MenuItems { get; set; }

        public ICommand Command { get; }
    }

    public class CommandViewModel : ICommand
    {
        private readonly Action action;

        public CommandViewModel(Action action)
        {
            this.action = action;
        }

        public void Execute(object? o)
        {
            action();
        }

        public bool CanExecute(object? o)
        {
            return true;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }
    }
}