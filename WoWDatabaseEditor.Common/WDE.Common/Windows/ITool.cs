using System.ComponentModel;
using AsyncAwaitBestPractices.MVVM;
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
        bool CanClose() => true;
    }

    [NonUniqueProvider]
    public interface ISavableTool : ITool
    {
        bool IsModified { get; }
        IAsyncCommand Save { get; }
    }

    public interface IFocusableTool : ITool
    {
        
    }

    public enum ToolPreferedPosition
    {
        Left, Right, Bottom, DocumentCenter
    }
}