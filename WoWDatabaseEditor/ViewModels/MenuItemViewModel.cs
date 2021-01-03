using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace WoWDatabaseEditor.ViewModels
{
    public class MenuItemViewModel
    {
        private readonly ICommand _command;

        public string Header { get; set; }

        public ObservableCollection<MenuItemViewModel> MenuItems { get; set; }

        public MenuItemViewModel(Action action, string header)
        {
            _command = new CommandViewModel(action);
            Header = header;
            MenuItems = new ObservableCollection<MenuItemViewModel>();
        }

        public ICommand Command => _command;
    }

    public class CommandViewModel : ICommand
    {
        private readonly Action _action;

        public CommandViewModel(Action action)
        {
            _action = action;
        }

        public void Execute(object? o)
        {
            _action();
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
