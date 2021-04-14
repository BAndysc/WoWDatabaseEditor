using System.ComponentModel;
using System.Windows;
using WDE.Module.Attributes;

namespace WDE.Common.Windows
{
    [NonUniqueProvider]
    public interface ITool : INotifyPropertyChanged
    {
        string Title { get; }
        string UniqueId { get; }
        bool Visibility { get; set; }
        ToolPreferedPosition PreferedPosition { get; }
        bool OpenOnStart { get; }
        bool IsSelected { get; set; }
    }

    public enum ToolPreferedPosition
    {
        Left, Right, Bottom
    }
}