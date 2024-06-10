using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncAwaitBestPractices.MVVM;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Sessions;
using WDE.Common.Solution;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.SqlQueryGenerator;

namespace WoWDatabaseEditor.Services.SolutionService
{
    [AutoRegister]
    [SingleInstance]
    public class SolutionTasksService : ObservableBase, ISolutionTasksService
    {
        private readonly ITaskRunner taskRunner;
        private readonly ISolutionItemSqlGeneratorRegistry sqlGenerator;
        private readonly ISolutionItemRemoteCommandGeneratorRegistry remoteCommandGenerator;
        private readonly IMySqlExecutor sqlExecutor;
        private readonly ISolutionItemNameRegistry solutionItemNameRegistry;
        private readonly IRemoteConnectorService remoteConnectorService;
        private readonly IDatabaseProvider databaseProvider;
        private readonly IStatusBar statusBar;
        private readonly ISessionService sessionService;
        private readonly IMessageBoxService messageBoxService;

        public SolutionTasksService(ITaskRunner taskRunner,
            ISolutionItemSqlGeneratorRegistry sqlGenerator,
            ISolutionItemRemoteCommandGeneratorRegistry remoteCommandGenerator,
            IMySqlExecutor sqlExecutor,
            ISolutionItemNameRegistry solutionItemNameRegistry,
            IRemoteConnectorService remoteConnectorService,
            IDatabaseProvider databaseProvider,
            IStatusBar statusBar,
            ISessionService sessionService,
            IMessageBoxService messageBoxService)
        {
            this.taskRunner = taskRunner;
            this.sqlGenerator = sqlGenerator;
            this.remoteCommandGenerator = remoteCommandGenerator;
            this.sqlExecutor = sqlExecutor;
            this.solutionItemNameRegistry = solutionItemNameRegistry;
            this.remoteConnectorService = remoteConnectorService;
            this.databaseProvider = databaseProvider;
            this.statusBar = statusBar;
            this.sessionService = sessionService;
            this.messageBoxService = messageBoxService;
            this.remoteConnectorService.EditorConnected += RemoteConnectorStateChanged;
            this.remoteConnectorService.EditorDisconnected += RemoteConnectorStateChanged;
        }

        private void RemoteConnectorStateChanged()
        {
            RaisePropertyChanged(nameof(CanReloadRemotely));
            RaisePropertyChanged(nameof(CanSaveAndReloadRemotely));
        }

        private Task SaveSolutionToDatabaseTask(ISolutionItem item, ISolutionItemDocument? document, Func<ISolutionItem, ISolutionItemDocument?, Task<IQuery>> queryGenerator)
        {
            if (!CanSaveToDatabase)
                return Task.CompletedTask;
            var itemName = solutionItemNameRegistry.GetName(item);
            
            return taskRunner.ScheduleTask($"Export {itemName} to database",
                async progress =>
                {
                    progress.Report(0, 2, "Generate query");
                    var query = await queryGenerator(item, document);
                    progress.Report(1, 2, "Execute query");
                    try
                    {
                        await sqlExecutor.ExecuteSql(query);
                    }
                    catch (IMySqlExecutor.QueryFailedDatabaseException e)
                    {
                        statusBar.PublishNotification(new PlainNotification(NotificationType.Error, "Couldn't apply SQL: " + e.Message));
                        throw;
                    }
                    progress.ReportFinished();
                });
        }

        public async Task SaveSolutionToDatabaseTask(ISolutionItemDocument document)
        {
            await SaveSolutionToDatabaseTask(document.SolutionItem, document, (_, doc) => doc!.GenerateQuery());
            if (document is not ISolutionItemManualUpdateSessionOnSave)
                await sessionService.UpdateQuery(document);
        }
        
        public Task SaveSolutionToDatabaseTask(ISolutionItem i)
        {
            return SaveSolutionToDatabaseTask(i, null, (item, _) => sqlGenerator.GenerateSql(item));
        }

        public Task ReloadSolutionRemotelyTask(ISolutionItem item)
        {
            var itemName = solutionItemNameRegistry.GetName(item);
            
            return taskRunner.ScheduleTask($"Reload {itemName} on server",
                async progress =>
                {
                    var commands = remoteCommandGenerator.GenerateCommand(item);
                    var reduced = remoteConnectorService.Merge(commands);

                    if (reduced.Count == 0)
                    {
                        progress.ReportFinished();
                        return;
                    }

                    try
                    {
                        for (int i = 0; i < reduced.Count; ++i)
                        {
                            var command = reduced[i].GenerateCommand();
                            progress.Report(i, reduced.Count, command);
                            var result = await remoteConnectorService.ExecuteCommand(reduced[i]);
                            if (result.Contains("ERROR:SQL(p)"))
                            {
                                var lines = result.Split('\n');
                                var filteredLines = lines.Where(l => l.StartsWith("ERROR:SQL(p)"))
                                    .Select(l => l.Substring("ERROR:SQL(p)".Length));
                                var message = string.Join("\n", filteredLines);
                                messageBoxService.SimpleDialog("Error", "Error while executing command: " + command, message).ListenErrors();
                            }
                        }
                    }
                    catch (CouldNotConnectToRemoteServer)
                    {
                        // we are fine with that
                    }
                    
                    progress.ReportFinished();
                });
        }

        public Task SaveAndReloadSolutionTask(ISolutionItem item)
        {
            var itemName = solutionItemNameRegistry.GetName(item);
            return taskRunner.ScheduleTask($"Save and reload {itemName} on server",
                async progress =>
                {
                    progress.Report(0, 1, "Generate query");

                    var commands = remoteCommandGenerator.GenerateCommand(item);
                    var reduced = remoteConnectorService.Merge(commands);
                    var query = await sqlGenerator.GenerateSql(item);

                    var sqlCount = Math.Max(1, reduced.Count);
                    var reloadCount = reduced.Count;
                    var totalCount = sqlCount + reloadCount;
                    
                    progress.Report(0, totalCount, "Update database");
                    await sqlExecutor.ExecuteSql(query);
                    
                    for (int i = 0; i < reduced.Count; ++i)
                    {
                        progress.Report(i + sqlCount, totalCount, reduced[i].GenerateCommand());
                        try
                        {
                            await remoteConnectorService.ExecuteCommand(reduced[i]);
                        }
                        catch (System.Net.Http.HttpRequestException)
                        {
                            statusBar.PublishNotification(new PlainNotification(NotificationType.Error,
                                "Unable to connect to server over SOAP. Check your settings or server settings."));
                            progress.ReportFail();
                            return;
                        }
                        catch (System.Threading.Tasks.TaskCanceledException)
                        {
                            statusBar.PublishNotification(new PlainNotification(NotificationType.Error,
                                "Unable to connect to server over SOAP. Check your settings or server settings."));
                            progress.ReportFail();
                            return;
                        }
                        catch (Exception e)
                        {
                            statusBar.PublishNotification(new PlainNotification(NotificationType.Error,
                                "Unable to connect to the server: " + e.Message));
                            progress.ReportFail();
                            return;
                        }
                    }
                    
                    progress.ReportFinished();
                });
        }

        public async Task Save(ISolutionItemDocument document)
        {
            if (document.Save?.CanExecute(null) ?? false)
            {
                if (document is IBeforeSaveConfirmDocument confirm)
                {
                    if (await confirm.ShallSavePreventClosing())
                        return;
                }
                if (document.Save is IAsyncCommand async)
                    await async.ExecuteAsync();
                else
                    document.Save.Execute(null);

                if (document is not ISolutionItemManualUpdateSessionOnSave)
                    await sessionService.UpdateQuery(document);
                
                if (CanReloadRemotely)
                    await ReloadSolutionRemotelyTask(document.SolutionItem);
            }
            else
            {
                if (CanSaveAndReloadRemotely)
                {
                    await SaveAndReloadSolutionTask(document.SolutionItem);
                    if (document is not ISolutionItemManualUpdateSessionOnSave)
                        await sessionService.UpdateQuery(document);
                }
                else if (CanSaveToDatabase)
                    await SaveSolutionToDatabaseTask(document);
            }
        }

        public bool CanSaveToDatabase => databaseProvider.IsConnected;
        public bool CanReloadRemotely => remoteConnectorService.IsConnected;
        public bool CanSaveAndReloadRemotely => CanSaveToDatabase && CanReloadRemotely;
    }
}