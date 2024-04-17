using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using WDE.Common.History;
using WDE.Common.Outliner;
using WDE.Common.Types;
using WDE.SqlQueryGenerator;

namespace WDE.Common.Managers
{
    public interface IUndoRedoWindow
    {
        ICommand Undo { get; }
        ICommand Redo { get; }
        IHistoryManager? History { get; }
        bool IsModified { get; }
    }

    public interface IBeforeSaveConfirmDocument
    {
        Task<bool> ShallSavePreventClosing();
    }

    public interface IDocument : IUndoRedoWindow, IDisposable, INotifyPropertyChanged
    {
        string Title { get; }
        ImageUri? Icon => null;
        ICommand Copy { get; }
        ICommand Cut { get; }
        ICommand Paste { get; }
        IAsyncCommand Save { get; }
        IAsyncCommand? CloseCommand { get; set; }
        bool CanClose { get; }
    }

    public interface ISolutionItemDocument : IDocument
    {
        ISolutionItem SolutionItem { get; }
        Task<IQuery> GenerateQuery();
        bool ShowExportToolbarButtons => true;
        bool IsLoading => false;
    }

    // this is a hack for quest chains
    public interface ISolutionItemManualUpdateSessionOnSave
    {

    }

    public interface IPeriodicSnapshotDocument : IDocument
    {
        string? TakeSnapshot();
        void RestoreSnapshot(string snapshot);
    }

    public interface ISplitSolutionItemQueryGenerator
    {
        Task<IList<(ISolutionItem, IQuery)>> GenerateSplitQuery();
    }

    public interface IProblemSourceDocument : IDocument
    {
        IObservable<IReadOnlyList<IInspectionResult>> Problems { get; }
    }

    public interface IOutlinerSourceDocument : IDocument
    {
        IObservable<IOutlinerModel> OutlinerModel { get; }
    }
    
    public interface IInspectionResult
    {
        public string Message { get; }
        public DiagnosticSeverity Severity { get; }
        public int Line { get; }
    }
    
    public enum DiagnosticSeverity
    {
        Critical,
        Error,
        Warning,
        Info
    }
}