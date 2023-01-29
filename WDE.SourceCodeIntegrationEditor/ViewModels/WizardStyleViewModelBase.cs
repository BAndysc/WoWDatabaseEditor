using System;
using System.Text;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Avalonia.Threading;
using Prism.Commands;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.SourceCodeIntegrationEditor.CoreSourceIntegration;

namespace WDE.SourceCodeIntegrationEditor.ViewModels
{
    public abstract class WizardStyleViewModelBase : ObservableBase
    {
        protected string coreSourceFolder = "";
        private uint wizardStep;
        private bool isLoading;

        public WizardStyleViewModelBase(
            IMessageBoxService messageBoxService,
            ICoreSourceSettings coreSourceSettings,
            ISourceSqlUpdateService sqlUpdateService,
            IAuthMySqlExecutor authExecutor,
            IMySqlExecutor worldExecutor,
            IWindowManager windowManager,
            ITaskRunner taskRunner,
            IStatusBar statusBar,
            INativeTextDocument resultCode)
        {
            ResultCode = resultCode;
            CoreSourceFolder = coreSourceSettings.CurrentCorePath;
            if (!string.IsNullOrEmpty(coreSourceSettings.CurrentCorePath))
                Dispatcher.UIThread.Post(() => WizardStep++);

            CommandPreviousStep = new DelegateCommand(() => WizardStep--, () => !IsLoading && WizardStep > 0)
                .ObservesProperty(() => IsLoading)
                .ObservesProperty(() => WizardStep);
            
            CommandNextStep = new DelegateCommand(() => WizardStep++, () => !IsLoading && WizardStep < TotalSteps - 1)
                .ObservesProperty(() => IsLoading)
                .ObservesProperty(() => WizardStep);

            PickCoreSourceFolder = new AsyncAutoCommand(async () =>
            {
                var selectedPath = await windowManager.ShowFolderPickerDialog(coreSourceFolder);
                if (selectedPath != null)
                {
                    if (!coreSourceSettings.SetCorePath(selectedPath))
                    {
                        await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                            .SetTitle("Invalid folder")
                            .SetMainInstruction("Invalid wow source folder")
                            .SetContent(
                                "It looks like it is not an valid wow server source folder.\n\nThe folder should contain src/ and sql/ subfolders")
                            .WithOkButton(true)
                            .Build());
                    }
                    CoreSourceFolder = coreSourceSettings.CurrentCorePath;
                }
            });
            
            ExecuteSqlCommand = new AsyncAutoCommand(async () =>
            {
                var (auth, world) = GenerateSql();
                try
                {
                    await taskRunner.ScheduleTask("Executing commands update", async () =>
                    {
                        statusBar.PublishNotification(new PlainNotification(NotificationType.Info, "Executing query"));
                        if (auth != null)
                        {
                            if (authExecutor.IsConnected)
                                await authExecutor.ExecuteSql(auth);
                            else
                            {
                                await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                                    .SetTitle("Auth database not connected")
                                    .SetIcon(MessageBoxIcon.Warning)
                                    .SetMainInstruction("Auth database not connected")
                                    .SetContent(
                                        "You are trying to execute auth query, but auth database is not connected.\n\nEnsure you have correct auth database data in settings.")
                                    .WithOkButton(true)
                                    .Build());
                            }
                        }

                        if (world != null)
                        {
                            if (worldExecutor.IsConnected)
                                await worldExecutor.ExecuteSql(world);
                            else
                            {
                                await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                                    .SetTitle("World database not connected")
                                    .SetIcon(MessageBoxIcon.Warning)
                                    .SetMainInstruction("World database not connected")
                                    .SetContent(
                                        "You are trying to execute world query, but world database is not connected.\n\nEnsure you have correct world database data in settings.")
                                    .WithOkButton(true)
                                    .Build());
                            }
                        }

                        statusBar.PublishNotification(new PlainNotification(NotificationType.Success,
                            "Executed query"));
                    });
                }
                catch (Exception)
                {
                    statusBar.PublishNotification(new PlainNotification(NotificationType.Error, "Error while executing query. Check Database Query Debug window"));
                }
            }, () => worldExecutor.IsConnected || authExecutor.IsConnected);
            
            SaveSqlCommand = new AsyncAutoCommand(async () =>
            {
                var (auth, world) = GenerateSql();

                if (auth != null)
                    sqlUpdateService.SaveAuthUpdate(SqlSuffixName, auth);
                
                if (world != null)
                    sqlUpdateService.SaveWorldUpdate(SqlSuffixName, world);

                if (auth != null || world != null)
                {
                    await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                        .SetTitle("Done!")
                        .SetContent("SQL files saved to sql/updates/")
                        .SetIcon(MessageBoxIcon.Information)
                        .WithOkButton(true)
                        .Build());   
                }
            });
        }
        
        protected string? CoreSourceFolder
        {
            get => string.IsNullOrEmpty(coreSourceFolder) ? null : coreSourceFolder;
            set => SetProperty(ref coreSourceFolder, value ?? "");
        }
        
        public INativeTextDocument ResultCode { get; }
        public ICommand CommandNextStep { get; }
        public ICommand CommandPreviousStep { get; }
        public ICommand PickCoreSourceFolder { get; }
        public ICommand SaveSqlCommand { get; }
        public ICommand ExecuteSqlCommand { get; }
        
        protected abstract int TotalSteps { get; }

        protected virtual void OnChangeStep(uint old, uint nnew)
        {
            if (old == TotalSteps - 2 && nnew == TotalSteps - 1)
            {
                StringBuilder sb = new();

                var (auth, world) = GenerateSql();
                if (auth != null)
                {
                    sb.AppendLine(" -- AUTH DATABASE");
                    sb.AppendLine(auth);
                }
                if (world != null)
                {
                    sb.AppendLine(" -- WORLD DATABASE");
                    sb.AppendLine(world);
                }
                ResultCode.FromString(sb.ToString());
            }
        }
        protected abstract (string? auth, string? world) GenerateSql();
        
        protected abstract string SqlSuffixName { get; }
        
        public uint WizardStep
        {
            get => wizardStep;
            set
            {
                var old = wizardStep;
                OnChangeStep(old, value);
                SetProperty(ref wizardStep, value);
            }
        }

        public bool IsLoading
        {
            get => isLoading;
            set => SetProperty(ref isLoading, value);
        }

        public ICommand Undo => AlwaysDisabledCommand.Command;
        public ICommand Redo => AlwaysDisabledCommand.Command;
        public ICommand Copy => AlwaysDisabledCommand.Command;
        public ICommand Cut => AlwaysDisabledCommand.Command;
        public ICommand Paste => AlwaysDisabledCommand.Command;
        public IAsyncCommand Save => AlwaysDisabledAsyncCommand.Command;
        public IAsyncCommand? CloseCommand { get; set; }
        public bool CanClose => true;
        public bool IsModified => false;
        public IHistoryManager? History => null;
    }
}