using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Sessions;
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
            INativeTextDocument nativeTextDocument,
            IQueryParserService queryParserService,
            ISessionService sessionService,
            IMessageBoxService messageBoxService)
        {
            Extension = "txt";
            Title = "New file";
            this.statusBar = statusBar;
            document = nativeTextDocument;

            SaveCommand = new AsyncAutoCommand(async () =>
            {
                var path = await windowManager.ShowSaveFileDialog($"{Extension} file|{Extension}|All files|*");
                if (path != null)
                    await File.WriteAllTextAsync(path, document.ToString());
            });
            ExecuteSqlSaveSession = new DelegateCommand(() =>
            {
                taskRunner.ScheduleTask("Executing query",
                 () => WrapStatusbar(async () =>
                 {
                     var query = Document.ToString();
                     IList<ISolutionItem>? solutionItems = null;
                     IList<string>? errors = null;
                     if (inspectQuery && sessionService.IsOpened && !sessionService.IsPaused)
                     {
                         (solutionItems, errors) = await queryParserService.GenerateItemsForQuery(query);
                     }

                     await mySqlExecutor.ExecuteSql(query);

                     if (solutionItems != null)
                     {
                         foreach (var item in solutionItems)
                             await sessionService.UpdateQuery(item);
                         if (errors!.Count > 0)
                         {
                             await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                                 .SetTitle("Apply query")
                                 .SetMainInstruction("Some queries couldn't be transformed into session items")
                                 .SetContent("Details:\n\n" + string.Join("\n", errors.Select(s => "  - " + s)))
                                 .WithOkButton(true)
                                 .Build());
                         }
                     }
                 }));
            }, () => databaseProvider.IsConnected);
            ExecuteSql = new DelegateCommand(() =>
            {
                taskRunner.ScheduleTask("Executing query",
                     () => WrapStatusbar(() => mySqlExecutor.ExecuteSql(Document.ToString())));
            }, () => databaseProvider.IsConnected);
        }

        private async Task WrapStatusbar(Func<Task> action)
        {
            statusBar.PublishNotification(new PlainNotification(NotificationType.Info, "Executing query"));
            try
            {
                await action();
                statusBar.PublishNotification(new PlainNotification(NotificationType.Success, "Query executed"));
            }
            catch (Exception e)
            {
                statusBar.PublishNotification(new PlainNotification(NotificationType.Error, "Failure during query execution: " + e.Message));
                Console.WriteLine(e);
            }
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
        public ICommand Save => AlwaysDisabledCommand.Command;
        public IAsyncCommand? CloseCommand { get; set; } = null;
        public bool CanClose => true;
        public bool IsModified => false;
        public IHistoryManager? History => null;
        public ImageUri? Icon { get; set; }
        public bool ShowExportToolbarButtons => false;
    }
}