using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Prism.Commands;
using WDE.Common.Managers;
using WDE.Common.Services.MessageBox;
using WDE.MVVM;

namespace WoWDatabaseEditorCore.Avalonia.Services.MessageBoxService
{
    internal class MessageBoxViewModel<T> : ObservableBase, IClosableDialog
    {
        public IMessageBox<T> Model { get; }

        public MessageBoxViewModel(IMessageBox<T> model)
        {
            Model = model;
            CancelButtonCommand = YesButtonCommand = NoButtonCommand = new DelegateCommand(() => { }, () => false);
            foreach (var btn in model.Buttons)
            {
                var vm = new MessageBoxButtonViewModel(btn.Name, btn == model.DefaultButton, new DelegateCommand(() =>
                {
                    SelectedOption = btn.ReturnValue;
                    Close?.Invoke();
                }));
                Buttons.Add(vm);
                if (btn == model.CancelButton)
                    CancelButtonCommand = vm.Command;
                else if (btn.Name == "Yes")
                    YesButtonCommand = vm.Command;
                else if (btn.Name == "No")
                    NoButtonCommand = vm.Command;
            }
        }

        public ICommand CancelButtonCommand { get; }
        public ICommand YesButtonCommand { get; }
        public ICommand NoButtonCommand { get; }
        
        public T? SelectedOption { get; private set; }
        public ObservableCollection<MessageBoxButtonViewModel> Buttons { get; } = new();

        public void OnClose()
        {
            CancelButtonCommand.Execute(null);
        }

        public event Action? Close;
    }

    internal class MessageBoxButtonViewModel : INotifyPropertyChanged
    {
        public MessageBoxButtonViewModel(string name,  bool isDefault, ICommand command)
        {
            Name = name;
            Command = command;
            IsDefault = isDefault;
        }

        public string Name { get; }
        public ICommand Command { get; }
        public bool IsDefault { get; }
        
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    
    internal class MessageBoxViewModelDesignModel : MessageBoxViewModel<bool>
    {
        public MessageBoxViewModelDesignModel(IMessageBox<bool> model) : base(model)
        {
        }
    }
}