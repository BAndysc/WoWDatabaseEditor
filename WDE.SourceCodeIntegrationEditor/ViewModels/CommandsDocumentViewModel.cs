using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Commands;
using WDE.Common.Database;
using WDE.Common.Documents;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.SourceCodeIntegrationEditor.CoreSourceIntegration;
using WDE.SourceCodeIntegrationEditor.CoreSourceIntegration.SourceParsers;
using WDE.SourceCodeIntegrationEditor.Generators;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Local

namespace WDE.SourceCodeIntegrationEditor.ViewModels
{
    [AutoRegister]
    public class CommandsDocumentViewModel : ObservableBase, IWizard
    {
        private static uint StepTcPath = 0;
        private static uint StepPickItems = 1;
        private static uint StepDetails = 2;
        private static uint StepFinal = 3;
        
        private readonly IAuthDatabaseProvider authDatabaseProvider;
        private readonly IDatabaseProvider databaseProvider;
        private readonly IRbacSourceParser rbacSourceParser;
        private readonly ICommandsSourceParser commandsSourceParser;
        private readonly ICommandSqlGenerator sqlGenerator;

        public ObservableCollection<CommandViewModel> CommandsNoRbac { get; } = new();
        public ObservableCollection<CommandViewModel> Commands { get; } = new();
        public ObservableCollection<CommandViewModel> ChosenCommands { get; } = new();

        public ObservableCollection<PermissionViewModel> PossibleParentPermissions { get; } = new();

        private string coreSourceFolder = "";
        private string? CoreSourceFolder
        {
            get => string.IsNullOrEmpty(coreSourceFolder) ? null : coreSourceFolder;
            set => SetProperty(ref coreSourceFolder, value ?? "");
        }
        
        public ICommand CommandNextStep { get; }
        public ICommand CommandPreviousStep { get; }
        public ICommand SaveSqlCommand { get; }
        public ICommand ExecuteSqlCommand { get; }
        public ICommand PickCoreSourceFolder { get; }
        
        public CommandsDocumentViewModel(IAuthDatabaseProvider authDatabaseProvider,
            IDatabaseProvider databaseProvider,
            IMessageBoxService messageBoxService,
            ICoreSourceSettings coreSourceSettings,
            IRbacSourceParser rbacSourceParser,
            ICommandsSourceParser commandsSourceParser,
            ICommandSqlGenerator sqlGenerator,
            ISourceSqlUpdateService sqlUpdateService,
            IAuthMySqlExecutor authExecutor,
            IMySqlExecutor worldExecutor,
            IWindowManager windowManager,
            ITaskRunner taskRunner,
            IStatusBar statusBar,
            INativeTextDocument resultCode)
        {
            this.authDatabaseProvider = authDatabaseProvider;
            this.databaseProvider = databaseProvider;
            this.rbacSourceParser = rbacSourceParser;
            this.commandsSourceParser = commandsSourceParser;
            this.sqlGenerator = sqlGenerator;
            ResultCode = resultCode;

            CoreSourceFolder = coreSourceSettings.CurrentCorePath;
            if (!string.IsNullOrEmpty(coreSourceSettings.CurrentCorePath))
                WizardStep = StepPickItems;

            CommandPreviousStep = new DelegateCommand(() => WizardStep--, () => !IsLoading && WizardStep > 0)
                .ObservesProperty(() => IsLoading)
                .ObservesProperty(() => WizardStep);
            
            CommandNextStep = new DelegateCommand(() => WizardStep++, () => !IsLoading && WizardStep < TotalSteps - 1)
                .ObservesProperty(() => IsLoading)
                .ObservesProperty(() => WizardStep);

            ExecuteSqlCommand = new AsyncAutoCommand(async () =>
            {
                var auth = sqlGenerator.GenerateAuth(ChosenCommands);
                var world = sqlGenerator.GenerateWorld(ChosenCommands);
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
            }, _ => worldExecutor.IsConnected || authExecutor.IsConnected);

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
            
            SaveSqlCommand = new AsyncAutoCommand(async () =>
            {
                var auth = sqlGenerator.GenerateAuth(ChosenCommands);
                var world = sqlGenerator.GenerateWorld(ChosenCommands);

                if (auth != null)
                    sqlUpdateService.SaveAuthUpdate("commands", auth);
                
                if (world != null)
                    sqlUpdateService.SaveWorldUpdate("commands", world);

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

        private async Task Load()
        {
            IsLoading = true;

            var permissions = await authDatabaseProvider.GetRbacPermissionsAsync();
            var links = await authDatabaseProvider.GetLinkedPermissionsAsync();

            Dictionary<uint, string> rbacEnumMapping = rbacSourceParser.ParseRbacEnum();
            Dictionary<string, uint> reverseRbacEnumMapping =
                rbacEnumMapping.ToDictionary(pair => pair.Value, pair => pair.Key);

            var databaseDefinedRbac = new HashSet<uint>(permissions.Select(p => p.Id));
            
            Dictionary<uint, PermissionViewModel> permissionViewModels = permissions.ToDictionary(p => p.Id, p =>
            {
                rbacEnumMapping.TryGetValue(p.Id, out var enumName);
                return new PermissionViewModel(p.Id, enumName ?? "UNKNOWN_ENUM", p);
            });
            
            PossibleParentPermissions.Clear();
            PossibleParentPermissions.AddRange(permissionViewModels.Values
                .Where(p => p.PermissionReadableName.StartsWith("Role:") && p.PermissionReadableName.EndsWith("Commands")));
            
            foreach (var pair in rbacEnumMapping)
            {
                if (!permissionViewModels.ContainsKey(pair.Key))
                    permissionViewModels[pair.Key] = new PermissionViewModel(pair.Key, pair.Value, "");
            }

            foreach (var link in links)
            {
                var parent = permissionViewModels[link.Id];
                var child = permissionViewModels[link.LinkedId];
                child.Parent = parent;
            }

            Dictionary<string, string?> existing = databaseProvider.GetCommands().ToDictionary(d => d.Name, d => d.Help);

            Commands.Clear();
            CommandsNoRbac.Clear();
            foreach (var cmd in commandsSourceParser.GetAllCommands())
            {
                if (!reverseRbacEnumMapping.TryGetValue(cmd.RbacPermission, out var rbacId))
                    continue;
                
                if (!permissionViewModels.TryGetValue(rbacId, out var permission))
                    continue;

                var hasDatabaseRbac = databaseDefinedRbac.Contains(rbacId);
                var hasDatabaseCommand = existing.ContainsKey(cmd.Command);

                if (!hasDatabaseRbac || !hasDatabaseCommand)
                {
                    var vm = new CommandViewModel(cmd.Command, permission);
                    if (vm.IsRbacDefined)
                        Commands.Add(vm);
                    else
                    {
                        vm.PermissionViewModel.PermissionReadableName = "Command: " + vm.Name;
                        CommandsNoRbac.Add(vm);
                    }
                }
            }
            
            IsLoading = false;
        }

        private uint wizardStep;
        public uint WizardStep
        {
            get => wizardStep;
            set
            {
                var old = wizardStep;
                if (old == StepTcPath && value == StepPickItems)
                    Load();
                if (old == StepPickItems && value == StepDetails)
                {
                    ChosenCommands.Clear();
                    ChosenCommands.AddRange(CommandsNoRbac.Where(c => c.IsSelected));
                    ChosenCommands.AddRange(Commands.Where(c => c.IsSelected));
                }

                if (old == StepDetails && value == StepFinal)
                {
                    StringBuilder sb = new();

                    var auth = sqlGenerator.GenerateAuth(ChosenCommands);
                    var world = sqlGenerator.GenerateWorld(ChosenCommands);
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
                SetProperty(ref wizardStep, value);
            }
        }
        
        private bool isLoading;
        public bool IsLoading
        {
            get => isLoading;
            set => SetProperty(ref isLoading, value);
        }
        
        public INativeTextDocument ResultCode { get; }
        private static int TotalSteps => 4;
        
        public string Title => "Commands wizard";
        public ImageUri? Icon => new ImageUri("icons/document_rbac.png");
        public ICommand Undo => AlwaysDisabledCommand.Command;
        public ICommand Redo => AlwaysDisabledCommand.Command;
        public ICommand Copy => AlwaysDisabledCommand.Command;
        public ICommand Cut => AlwaysDisabledCommand.Command;
        public ICommand Paste => AlwaysDisabledCommand.Command;
        public ICommand Save => AlwaysDisabledCommand.Command;
        public IAsyncCommand? CloseCommand { get; set; }
        public bool CanClose => true;
        public bool IsModified => false;
        public IHistoryManager? History => null;
    }
}