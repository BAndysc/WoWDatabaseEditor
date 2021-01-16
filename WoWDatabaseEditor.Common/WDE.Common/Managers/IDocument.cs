using System;
using System.Windows.Input;
using WDE.Common.History;

namespace WDE.Common.Managers
{
    public interface IDocument : IDisposable
    {
        string Title { get; }
        ICommand Undo { get; }
        ICommand Redo { get; }
        ICommand Copy { get; }
        ICommand Cut { get; }
        ICommand Paste { get; }
        ICommand Save { get; }
        ICommand CloseCommand { get; set; }
        bool CanClose { get; }
        bool IsModified { get; }
        IHistoryManager History { get; }
    }
}