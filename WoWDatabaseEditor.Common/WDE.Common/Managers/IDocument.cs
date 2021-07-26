using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using WDE.Common.Annotations;
using WDE.Common.History;
using WDE.Common.Types;

namespace WDE.Common.Managers
{
    public interface IDocument : IDisposable, INotifyPropertyChanged
    {
        string Title { get; }
        ImageUri? Icon => null;
        ICommand Undo { get; }
        ICommand Redo { get; }
        ICommand Copy { get; }
        ICommand Cut { get; }
        ICommand Paste { get; }
        ICommand Save { get; }
        IAsyncCommand? CloseCommand { get; set; }
        bool CanClose { get; }
        bool IsModified { get; }
        IHistoryManager? History { get; }
    }

    public interface ISolutionItemDocument : IDocument
    {
        ISolutionItem SolutionItem { get; }
        bool ShowExportToolbarButtons => true;
    }

    public interface IProblemSourceDocument : IDocument
    {
        IObservable<IReadOnlyList<IInspectionResult>> Problems { get; }
    }

    public interface IInspectionResult
    {
        public string Message { get; }
        public DiagnosticSeverity Severity { get; }
        public int Line { get; }
    }
    
    public enum DiagnosticSeverity
    {
        Error,
        Warning,
        Info
    }
}