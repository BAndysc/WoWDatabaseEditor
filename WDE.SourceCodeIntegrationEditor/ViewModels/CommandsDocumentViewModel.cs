using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Documents;
using WDE.Common.Managers;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.SourceCodeIntegrationEditor.CoreSourceIntegration;
using WDE.SourceCodeIntegrationEditor.CoreSourceIntegration.SourceParsers;
using WDE.SourceCodeIntegrationEditor.Generators;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Local

namespace WDE.SourceCodeIntegrationEditor.ViewModels
{
    [AutoRegister]
    public class CommandsDocumentViewModel : WizardStyleViewModelBase, IWizard
    {
        private static uint StepTcPath = 0;
        private static uint StepPickItems = 1;
        private static uint StepDetails = 2;
        
        private readonly IAuthDatabaseProvider authDatabaseProvider;
        private readonly IDatabaseProvider databaseProvider;
        private readonly IRbacSourceParser rbacSourceParser;
        private readonly ICommandsSourceParser commandsSourceParser;
        private readonly ICommandSqlGenerator sqlGenerator;
        public string Title => "Commands wizard";
        public ImageUri? Icon => new ImageUri("Icons/document_rbac.png");
        
        public ObservableCollection<CommandViewModel> CommandsNoRbac { get; } = new();
        public ObservableCollection<CommandViewModel> Commands { get; } = new();
        public ObservableCollection<CommandViewModel> ChosenCommands { get; } = new();

        public ObservableCollection<PermissionViewModel> PossibleParentPermissions { get; } = new();

        protected override int TotalSteps => 4;

        protected override (string? auth, string? world) GenerateSql() => (sqlGenerator.GenerateAuth(ChosenCommands), sqlGenerator.GenerateWorld(ChosenCommands));
        
        protected override string SqlSuffixName => "commands";
        
        protected override void OnChangeStep(uint old, uint nnew)
        {
            if (old == StepTcPath && nnew == StepPickItems)
                Load().ListenErrors();
            if (old == StepPickItems && nnew == StepDetails)
            {
                ChosenCommands.Clear();
                ChosenCommands.AddRange(CommandsNoRbac.Where(c => c.IsSelected));
                ChosenCommands.AddRange(Commands.Where(c => c.IsSelected));
            }
            base.OnChangeStep(old, nnew);
        }

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
            INativeTextDocument resultCode) : base(messageBoxService, coreSourceSettings, sqlUpdateService, authExecutor, worldExecutor, windowManager, taskRunner, statusBar, resultCode)
        {
            this.authDatabaseProvider = authDatabaseProvider;
            this.databaseProvider = databaseProvider;
            this.rbacSourceParser = rbacSourceParser;
            this.commandsSourceParser = commandsSourceParser;
            this.sqlGenerator = sqlGenerator;
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

            Dictionary<string, string?> existing = (await databaseProvider.GetCommands()).ToDictionary(d => d.Name, d => d.Help);

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
    }
}