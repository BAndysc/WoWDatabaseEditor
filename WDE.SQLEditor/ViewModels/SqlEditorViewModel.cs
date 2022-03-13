﻿using System;
using System.Text;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Solution;
using WDE.Common.Tasks;
using WDE.Common.Types;
using WDE.Common.Utils;
using IDocument = WDE.Common.Managers.IDocument;

namespace WDE.SQLEditor.ViewModels
{
    public class SqlEditorViewModel : BindableBase, IDocument, IDialog
    {
        private INativeTextDocument code;
        public ImageUri? Icon { get; } = new("Icons/document_sql.png");

        private SqlEditorViewModel(IMySqlExecutor mySqlExecutor, 
            IStatusBar statusBar, 
            IDatabaseProvider databaseProvider,
            ITaskRunner taskRunner, 
            INativeTextDocument sql)
        {
            Code = sql;
            ExecuteSql = new DelegateCommand(() =>
            {
                taskRunner.ScheduleTask("Executing query",
                    async () =>
                    {
                        statusBar.PublishNotification(new PlainNotification(NotificationType.Info, "Executing query"));
                        try
                        {
                            await mySqlExecutor.ExecuteSql(Code.ToString());
                            statusBar.PublishNotification(new PlainNotification(NotificationType.Success, "Query executed"));
                        }
                        catch (Exception e)
                        {
                            statusBar.PublishNotification(new PlainNotification(NotificationType.Error, "Failure during query execution"));
                            Console.WriteLine(e);
                        }
                    });
            }, () => databaseProvider.IsConnected);
            IsLoading = false;
            Save = new DelegateCommand(() =>
            {
                ExecuteSql.Execute(null);
            });
            Accept = Cancel = new DelegateCommand(() => CloseOk?.Invoke());
        }

        public SqlEditorViewModel(IMySqlExecutor mySqlExecutor, 
            IStatusBar statusBar,
            IDatabaseProvider databaseProvider, 
            ITaskRunner taskRunner, 
            ISolutionItemSqlGeneratorRegistry sqlGeneratorsRegistry,
            INativeTextDocument sql,
            MetaSolutionSQL item) : this(mySqlExecutor, statusBar, databaseProvider, taskRunner, sql)
        {
            IsLoading = true;
            taskRunner.ScheduleTask("Generating SQL",
                async () =>
                {
                    StringBuilder sb = new();
                    foreach (var subitem in item.ItemsToGenerate)
                        sb.AppendLine((await sqlGeneratorsRegistry.GenerateSql(subitem)).QueryString);
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

        public ICommand ExecuteSql { get; }

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
        public event Action CloseCancel;
        public event Action CloseOk;
        public ICommand Undo { get; } = AlwaysDisabledCommand.Command;
        public ICommand Redo { get; } = AlwaysDisabledCommand.Command;
        public ICommand Copy { get; } = AlwaysDisabledCommand.Command;
        public ICommand Cut { get; } = AlwaysDisabledCommand.Command;
        public ICommand Paste { get; } = AlwaysDisabledCommand.Command;
        public ICommand Save { get; }
        public IAsyncCommand CloseCommand { get; set; } = null;
        public bool CanClose { get; } = true;
        public bool IsModified { get; } = false;
        public IHistoryManager History { get; } = null;
    }
}
