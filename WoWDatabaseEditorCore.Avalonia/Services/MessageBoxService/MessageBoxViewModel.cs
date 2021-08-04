using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Prism.Commands;
using WDE.Common.Services.MessageBox;
using WDE.MVVM;

namespace WoWDatabaseEditorCore.Avalonia.Services.MessageBoxService
{
    internal interface IMessageBoxViewModel
    {
        public event Action Close;
    }
    
    internal class MessageBoxViewModel<T> : ObservableBase, IMessageBoxViewModel
    {
        public IMessageBox<T> Model { get; }

        public MessageBoxViewModel(IMessageBox<T> model)
        {
            Model = model;
            foreach (var btn in model.Buttons)
            {
                Buttons.Add(new MessageBoxButtonViewModel(btn.Name, new DelegateCommand(() =>
                {
                    SelectedOption = btn.ReturnValue;
                    Close?.Invoke();
                })));
            }
        }

        public T? SelectedOption { get; private set; }
        public ObservableCollection<MessageBoxButtonViewModel> Buttons { get; } = new();
        public event Action? Close;
    }

    internal class MessageBoxButtonViewModel : INotifyPropertyChanged
    {
        public MessageBoxButtonViewModel(string name, ICommand command)
        {
            Name = name;
            Command = command;
        }

        public string Name { get; }
        public ICommand Command { get; }
        
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    
    internal class MessageBoxViewModelDesignModel : MessageBoxViewModel<bool>
    {
        public MessageBoxViewModelDesignModel(IMessageBox<bool> model) : base(model)
        {
        }
    }
}