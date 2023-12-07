using System.IO;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Mvvm;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.ViewModels
{
    [AutoRegister]
    public class TextDocumentViewModel : BindableBase, IDocument
    {
        public TextDocumentViewModel(IWindowManager windowManager,
            IStatusBar statusBar,
            IDatabaseProvider databaseProvider,
            INativeTextDocument nativeTextDocument,
            ITextDocumentService textDocumentService)
        {
            Extension = "txt";
            Title = "New file";
            this.statusBar = statusBar;
            document = nativeTextDocument;
            this.textDocumentService = textDocumentService;

            SaveCommand = new AsyncAutoCommand(async () =>
            {
                var path = await windowManager.ShowSaveFileDialog($"{Extension} file|{Extension}|All files|*");
                if (path != null)
                    await File.WriteAllTextAsync(path, document.ToString());
            });
            ExecuteSqlSaveSession = new AsyncAutoCommand(() => textDocumentService.ExecuteSqlSaveSession(Document.ToString(), inspectQuery),
                () => databaseProvider.IsConnected);
            ExecuteSql = new AsyncAutoCommand(() => this.textDocumentService.ExecuteSql(Document.ToString()),
                () => databaseProvider.IsConnected);
        }

        public TextDocumentViewModel Set(string title, string content, string extension = "txt", bool inspectQuery = false)
        {
            this.inspectQuery = inspectQuery;
            Title = title;
            Extension = extension;
            Icon = new ImageUri($"Icons/document_{extension.ToLower()}.png");
            document.FromString(content);
            return this;
        }
        
        public void Dispose()
        {
        }

        private readonly IStatusBar statusBar;
        private INativeTextDocument document;
        private readonly ITextDocumentService textDocumentService;

        public INativeTextDocument Document
        {
            get => document;
            set => SetProperty(ref document, value);
        }

        private bool inspectQuery;
        public bool IsSqlText => Extension == "sql";
        public ICommand ExecuteSqlSaveSession { get; }
        public ICommand ExecuteSql { get; }
        public string Extension { get; set; }
        public string Title { get; set; }
        public ICommand SaveCommand { get; }
        public ICommand Undo => AlwaysDisabledCommand.Command;
        public ICommand Redo => AlwaysDisabledCommand.Command;
        public ICommand Copy => AlwaysDisabledCommand.Command;
        public ICommand Cut => AlwaysDisabledCommand.Command;
        public ICommand Paste => AlwaysDisabledCommand.Command;
        public IAsyncCommand Save => AlwaysDisabledAsyncCommand.Command;
        public IAsyncCommand? CloseCommand { get; set; } = null;
        public bool CanClose => true;
        public bool IsModified => false;
        public IHistoryManager? History => null;
        public ImageUri? Icon { get; set; }
        public bool ShowExportToolbarButtons => false;
    }
}