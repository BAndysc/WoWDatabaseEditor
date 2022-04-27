using System;
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
        public bool Shift { get; set; }

        public string InputShortcutText
        {
            get
            {
                if (Control && Shift)
                    return $"Ctrl+Shift+{Key}";
                else if (Control)
                    return $"Ctrl+{Key}";
                else if (Shift)
                    return $"Shift+{Key}";
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
                Shift = false;
            }
            else
            {
                var plus2 = key.IndexOf('+', plus + 1);
                if (plus2 == -1)
                {
                    Key = key.Substring(plus + 1);
                    Control = string.Equals(key.Substring(0, plus), "Control", StringComparison.OrdinalIgnoreCase);
                    Shift = string.Equals(key.Substring(0, plus), "Shift", StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    Key = key.Substring(plus2 + 1);
                    var firstKey = key.Substring(0, plus);
                    var secondKey = key.Substring(plus + 1, plus2 - plus - 1);
                    Control = firstKey == "Control" || secondKey == "Control";
                    Shift = firstKey == "Shift" || secondKey == "Shift";
                }
            }
        }
    }
}
