using System;
using System.IO;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Tasks;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.ViewModels
{
    [AutoRegister]
    public class TextDocumentViewModel : BindableBase, IDocument
    {
        public TextDocumentViewModel(IWindowManager windowManager,
            ITaskRunner taskRunner,
            IStatusBar statusBar,
            IMySqlExecutor mySqlExecutor,
            IDatabaseProvider databaseProvider,
            INativeTextDocument nativeTextDocument)
        {
            Extension = "txt";
            Title = "New file";
            document = nativeTextDocument;

            SaveCommand = new AsyncAutoCommand(async () =>
            {
                var path = await windowManager.ShowSaveFileDialog($"{Extension} file|{Extension}|All files|*");
                if (path != null)
                    await File.WriteAllTextAsync(path, document.ToString());
            });
            ExecuteSql = new DelegateCommand(() =>
            {
                taskRunner.ScheduleTask("Executing query",
                    async () =>
                    {
                        statusBar.PublishNotification(new PlainNotification(NotificationType.Info, "Executing query"));
                        try
                        {
                            await mySqlExecutor.ExecuteSql(Document.ToString());
                            statusBar.PublishNotification(new PlainNotification(NotificationType.Success, "Query executed"));
                        }
                        catch (Exception e)
                        {
                            statusBar.PublishNotification(new PlainNotification(NotificationType.Error, "Failure during query execution"));
                            Console.WriteLine(e);
                        }
                    });
            }, () => databaseProvider.IsConnected);
        }

        public TextDocumentViewModel Set(string title, string content, string extension = "txt")
        {
            Title = title;
            Extension = extension;
            Icon = new ImageUri($"icons/document_{extension.ToLower()}.png");
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

        public bool IsSqlText => Extension == "sql";
        public ICommand ExecuteSql { get; }
        public string Extension { get; set; }
        public string Title { get; set; }
        public ICommand SaveCommand { get; }
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
        public ImageUri? Icon { get; set; }
        public bool ShowExportToolbarButtons => false;
    }
}