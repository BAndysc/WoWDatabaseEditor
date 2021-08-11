using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using WDE.Common.Managers;

namespace WDE.Common.Menu
{
    public interface IMenuItem
    {
        string ItemName { get; }
    }

    public interface IMenuSeparator : IMenuItem
    {
        
    }

    public interface IMenuDocumentItem : IMenuItem
    {
        IDocument EditorDocument();
    }

    public interface IMenuCommandItem : IMenuItem
    {
        ICommand ItemCommand { get; }
        MenuShortcut? Shortcut { get; }
    }

    public interface ICheckableMenuItem : IMenuItem, INotifyPropertyChanged
    {
        public bool IsChecked { get; }
    }

    public interface IMenuCategoryItem: IMenuItem
    {
        List<IMenuItem> CategoryItems { get; }
    }

    public struct MenuShortcut
    {
        public string Key { get; set; }
        public bool Control { get; set; }

        public string InputShortcutText
        {
            get
            {
                if (Control)
                    return $"Ctrl+{Key}";
                else
                    return Key;
            }
        }

        public MenuShortcut(string key)
        {
            var plus = key.IndexOf('+');
            if (plus == -1)
            {
                Key = key;
                Control = false;
            }
            else
            {
                Key = key.Substring(plus + 1);
                Control = true;
            }
        }
    }
}
