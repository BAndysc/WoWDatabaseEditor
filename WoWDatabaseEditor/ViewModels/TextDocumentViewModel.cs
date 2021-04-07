using System.ComponentModel;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Mvvm;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.ViewModels
{
    [AutoRegister]
    public class TextDocumentViewModel : BindableBase, IDocument
    {
        public TextDocumentViewModel(INativeTextDocument nativeTextDocument)
        {
            Title = "New file";
            document = nativeTextDocument;
        }

        public TextDocumentViewModel Set(string title, string content)
        {
            Title = title;
            document.FromString(content);
            return this;
        }
        
        public void Dispose()
        {
        }

        private INativeTextDocument document;
        public INativeTextDocument Document
        {
            get => document;
            set => SetProperty(ref document, value);
        }
        public string Title { get; set; }
        public ICommand Undo => AlwaysDisabledCommand.Command;
        public ICommand Redo => AlwaysDisabledCommand.Command;
        public ICommand Copy => AlwaysDisabledCommand.Command;
        public ICommand Cut => AlwaysDisabledCommand.Command;
        public ICommand Paste => AlwaysDisabledCommand.Command;
        public ICommand Save => AlwaysDisabledCommand.Command;
        public IAsyncCommand? CloseCommand { get; set; } = null;
        public bool CanClose => true;
        public bool IsModified => false;
        public IHistoryManager? History => null;
    }
}