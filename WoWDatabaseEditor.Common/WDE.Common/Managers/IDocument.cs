using System;
using System.ComponentModel;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using WDE.Common.History;

namespace WDE.Common.Managers
{
    public interface IDocument : IDisposable, INotifyPropertyChanged
    {
        string Title { get; }
        ICommand Undo { get; }
        ICommand Redo { get; }
        ICommand Copy { get; }
        ICommand Cut { get; }
        ICommand Paste { get; }
        ICommand Save { get; }
        IAsyncCommand CloseCommand { get; set; }
        bool CanClose { get; }
        bool IsModified { get; }
        IHistoryManager History { get; }
    }

    public interface ISolutionItemDocument : IDocument
    {
        ISolutionItem SolutionItem { get; }
    }
}