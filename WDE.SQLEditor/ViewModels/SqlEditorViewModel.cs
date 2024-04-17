using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Commands;
using Prism.Mvvm;
using PropertyChanged.SourceGenerator;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Services.MessageBox;
using WDE.Common.Solution;
using WDE.Common.Tasks;
using WDE.Common.Types;
using WDE.Common.Utils;
using IDocument = WDE.Common.Managers.IDocument;

namespace WDE.SQLEditor.ViewModels
{
    public partial class SqlEditorViewModel : BindableBase, IDocument, IDialog
    {
        private INativeTextDocument code;
        public ImageUri? Icon { get; } = new("Icons/document_sql.png");

        [Notify] private DataDatabaseType database;

        public static IList<DataDatabaseType> DatabaseTypes { get; } = new List<DataDatabaseType>()
        {
            DataDatabaseType.World,
            DataDatabaseType.Hotfix
        };
        
        private SqlEditorViewModel(IMySqlExecutor mySqlExecutor, 
            IMySqlHotfixExecutor hotfixExecutor,
            IStatusBar statusBar, 
            IDatabaseProvider databaseProvider,
            ITaskRunner taskRunner, 
            INativeTextDocument sql)
        {
            code = sql;
            ExecuteSql = new AsyncAutoCommand(() =>
            {
                return taskRunner.ScheduleTask("Executing query",
                    async () =>
                    {
                        statusBar.PublishNotification(new PlainNotification(NotificationType.Info, $"Executing {database} query"));
                        try
                        {
                            if (database == DataDatabaseType.Hotfix)
                                await hotfixExecutor.ExecuteSql(Code.ToString());
                            else if (database == DataDatabaseType.World)
                                await mySqlExecutor.ExecuteSql(Code.ToString());
                            else
                                throw new Exception($"Unknown database type: {database}");
                            statusBar.PublishNotification(new PlainNotification(NotificationType.Success, $"{database} Query executed"));
                        }
                        catch (Exception e)
                        {
                            statusBar.PublishNotification(new PlainNotification(NotificationType.Error, $"Failure during {database} query execution"));
                            LOG.LogError(e, message: "Error during query execution");
                        }
                    });
            }, () => databaseProvider.IsConnected);
            IsLoading = false;
            Save = ExecuteSql;
            Accept = Cancel = new DelegateCommand(() => CloseOk?.Invoke());
        }

        public SqlEditorViewModel(IMySqlExecutor mySqlExecutor, 
            IMySqlHotfixExecutor hotfixExecutor,
            IStatusBar statusBar,
            IDatabaseProvider databaseProvider, 
            ITaskRunner taskRunner,
            IMessageBoxService messageBoxService,
            ISolutionItemSqlGeneratorRegistry sqlGeneratorsRegistry,
            INativeTextDocument sql,
            IMainThread mainThread,
            MetaSolutionSQL item) : this(mySqlExecutor, hotfixExecutor, statusBar, databaseProvider, taskRunner, sql)
        {
            IsLoading = true;
            taskRunner.ScheduleTask("Generating SQL",
                async () =>
                {
                    StringBuilder sb = new();
                    try
                    {
                        DataDatabaseType? previousType = null;
                        foreach (var subitem in item.ItemsToGenerate)
                        {
                            var q = await sqlGeneratorsRegistry.GenerateSql(subitem);
                            if (previousType.HasValue && q.Database != previousType)
                                messageBoxService.SimpleDialog("WARNING!!", "Warning!", "Somehow some queries in this SQL refers to World database and some refers to Hotfix. Be cautious before applying.").ListenErrors();
                            previousType = q.Database;
                            sb.AppendLine(q.QueryString);
                        }

                        Database = previousType ?? DataDatabaseType.World;
                    }
                    catch (Exception)
                    {
                        mainThread.Delay(() => CloseCommand?.ExecuteAsync(), TimeSpan.FromMilliseconds(1));
                        throw;
                    }
                    string sql = sb.ToString();
                    
                    Code.FromString(sql);
                    IsLoading = false;
                });
        }

        public INativeTextDocument Code
        {
            get => code;
            set => SetProperty(ref code, value);
        }

        public IAsyncCommand ExecuteSql { get; }

        public void Dispose()
        {
        }

        private bool isLoading = false;
        public bool IsLoading
        {
            get => isLoading;
            set => SetProperty(ref isLoading, value);
        }

        public int DesiredWidth => 700;
        public int DesiredHeight => 500;
        public string Title => "SQL Output";
        public bool Resizeable => true;
        public ICommand Accept { get; }
        public ICommand Cancel { get; }
        public event Action? CloseCancel;
        public event Action? CloseOk;
        public ICommand Undo { get; } = AlwaysDisabledCommand.Command;
        public ICommand Redo { get; } = AlwaysDisabledCommand.Command;
        public ICommand Copy { get; } = AlwaysDisabledCommand.Command;
        public ICommand Cut { get; } = AlwaysDisabledCommand.Command;
        public ICommand Paste { get; } = AlwaysDisabledCommand.Command;
        public IAsyncCommand Save { get; }
        public IAsyncCommand? CloseCommand { get; set; } = null;
        public bool CanClose { get; } = true;
        public bool IsModified { get; } = false;
        public IHistoryManager? History { get; } = null;
    }
}
