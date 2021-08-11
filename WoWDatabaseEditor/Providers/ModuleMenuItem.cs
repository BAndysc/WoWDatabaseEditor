using System;
using System.ComponentModel;
using System.Windows.Input;
using WDE.Common.Menu;
using WDE.MVVM.Observable;

namespace WoWDatabaseEditorCore.Providers
{
    public class ModuleMenuItem: IMenuCommandItem
    {
        public string ItemName { get; }
        public ICommand ItemCommand { get; }
        public MenuShortcut? Shortcut { get; }

        public ModuleMenuItem(string itemName, ICommand itemCommand, MenuShortcut? shortcut = null)
        {
            ItemName = itemName;
            ItemCommand = itemCommand;
            Shortcut = shortcut;
        }
    }
    
    public class CheckableModuleMenuItem : IMenuCommandItem, ICheckableMenuItem, System.IDisposable
    {
        public string ItemName { get; }
        public ICommand ItemCommand { get; }
        public MenuShortcut? Shortcut { get; }
        private System.IDisposable? sub;

        public CheckableModuleMenuItem(string itemName, IObservable<bool> isChecked, ICommand itemCommand, MenuShortcut? shortcut = null)
        {
            ItemName = itemName;
            ItemCommand = itemCommand;
            Shortcut = shortcut;
            sub = isChecked.SubscribeAction(@is =>
            {
                IsChecked = @is;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public bool IsChecked { get; private set; }

        public void Dispose()
        {
            sub?.Dispose();
            sub = null;
        }
    }
}