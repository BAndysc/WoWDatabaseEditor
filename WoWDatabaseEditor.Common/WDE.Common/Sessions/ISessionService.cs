using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using WDE.Common.Managers;
using WDE.Module.Attributes;

namespace WDE.Common.Sessions
{
    [UniqueProvider]
    public interface ISessionService : INotifyPropertyChanged, IEnumerable<IEditorSessionStub>, INotifyCollectionChanged
    {
        bool? DeleteOnSave { get; set; }
        bool IsPaused { get; set; }
        bool IsOpened { get; }
        bool IsNonEmpty { get; }
        void Open(IEditorSessionStub? stub);
        void Save();
        ISolutionItem? Find(ISolutionItem item);
        Task UpdateQuery(ISolutionItem solutionItem);
        Task UpdateQuery(ISolutionItemDocument documentItem);
        void BeginSession(string name);
        void Finalize(string fileName);
        string? GenerateCurrentQuery();
        
        ObservableCollection<IEditorSessionStub> DeletedSessions { get; }
        IEditorSession? CurrentSession { get; }
        void ForgetCurrent();
        void RemoveItem(ISolutionItem item);
        void RestoreSession(IEditorSessionStub stub);
        void ForgetSession(IEditorSessionStub stub);
    }

    [UniqueProvider]
    public interface ISessionServiceViewModel : IUndoRedoWindow
    {
        ICommand NewSessionCommand { get; }
        ICommand SaveCurrentCurrent { get; }
        ICommand ForgetCurrentCurrent { get; }
        ICommand PreviewCurrentCommand { get; }
        DelegateCommand<ISolutionItem> DeleteItem { get; }
        
        Task NewSession();
        Task SaveCurrent();
        Task ForgetCurrent();
    }

    public interface IEditorSessionStub
    {
        string Name { get; }
        string FileName { get; }
        DateTime Created { get; }
        DateTime LastModified { get; set; }
    }
    
    public interface IEditorSession : IEditorSessionStub, IEnumerable<(ISolutionItem, string)>, INotifyCollectionChanged
    {
        void Insert(ISolutionItem item, string query);
        ISolutionItem? Find(ISolutionItem item);
    }
}