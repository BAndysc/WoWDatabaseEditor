using System;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Solution;
using WDE.Common.Tasks;
using WDE.Module.Attributes;

namespace WoWDatabaseEditor.Services.SolutionService
{
    [AutoRegister]
    [SingleInstance]
    public class SolutionTasksService : ISolutionTasksService
    {
        private readonly ITaskRunner taskRunner;
        private readonly ISolutionItemSqlGeneratorRegistry sqlGenerator;
        private readonly ISolutionItemRemoteCommandGeneratorRegistry remoteCommandGenerator;
        private readonly IMySqlExecutor sqlExecutor;
        private readonly ISolutionItemNameRegistry solutionItemNameRegistry;
        private readonly IRemoteConnectorService remoteConnectorService;
        private readonly IDatabaseProvider databaseProvider;
        private readonly IStatusBar statusBar;

        public SolutionTasksService(ITaskRunner taskRunner,
            ISolutionItemSqlGeneratorRegistry sqlGenerator,
            ISolutionItemRemoteCommandGeneratorRegistry remoteCommandGenerator,
            IMySqlExecutor sqlExecutor,
            ISolutionItemNameRegistry solutionItemNameRegistry,
            IRemoteConnectorService remoteConnectorService,
            IDatabaseProvider databaseProvider,
            IStatusBar statusBar)
        {
            this.taskRunner = taskRunner;
            this.sqlGenerator = sqlGenerator;
            this.remoteCommandGenerator = remoteCommandGenerator;
            this.sqlExecutor = sqlExecutor;
            this.solutionItemNameRegistry = solutionItemNameRegistry;
            this.remoteConnectorService = remoteConnectorService;
            this.databaseProvider = databaseProvider;
            this.statusBar = statusBar;
        }
        
        public void SaveSolutionToDatabaseTask(ISolutionItem item)
        {
            if (!CanSaveToDatabase)
                return;
            var itemName = solutionItemNameRegistry.GetName(item);
            
            taskRunner.ScheduleTask($"Export {itemName} to database",
                async progress =>
                {
                    progress.Report(0, 2, "Generate query");
                    var query = await sqlGenerator.GenerateSql(item);
                    progress.Report(1, 2, "Execute query");
                    await sqlExecutor.ExecuteSql(query); 
                    progress.ReportFinished();
                });
        }

        public void ReloadSolutionRemotelyTask(ISolutionItem item)
        {
            var itemName = solutionItemNameRegistry.GetName(item);
            
            taskRunner.ScheduleTask($"Reload {itemName} on server",
                async progress =>
                {
                    var commands = remoteCommandGenerator.GenerateCommand(item);
                    var reduced = remoteConnectorService.Merge(commands);

                    if (reduced.Count == 0)
                    {
                        progress.ReportFinished();
                        return;
                    }

                    for (int i = 0; i < reduced.Count; ++i)
                    {
                        progress.Report(i, reduced.Count, reduced[i].GenerateCommand());
                        await remoteConnectorService.ExecuteCommand(reduced[i]);
                    }
                    
                    progress.ReportFinished();
                });
        }

        public void SaveAndReloadSolutionTask(ISolutionItem item)
        {
            var itemName = solutionItemNameRegistry.GetName(item);
            taskRunner.ScheduleTask($"Save and reload {itemName} on server",
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
                        catch (System.Net.Http.HttpRequestException e)
                        {
                            statusBar.PublishNotification(new PlainNotification(NotificationType.Error,
                                "Unable to connect to server over SOAP. Check your settings or server settings."));
                            progress.ReportFail();
                            return;
                        }
                        catch (System.Threading.Tasks.TaskCanceledException e)
                        {
                            statusBar.PublishNotification(new PlainNotification(NotificationType.Error,
                                "Unable to connect to server over SOAP. Check your settings or server settings."));
                            progress.ReportFail();
                            return;
                        }
                    }
                    
                    progress.ReportFinished();
                });
        }

        public bool CanSaveToDatabase => databaseProvider.IsConnected;
        public bool CanReloadRemotely => remoteConnectorService.IsConnected;
        public bool CanSaveAndReloadRemotely => CanSaveToDatabase && CanReloadRemotely;
    }
}