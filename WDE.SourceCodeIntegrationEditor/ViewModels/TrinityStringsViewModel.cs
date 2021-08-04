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

namespace WDE.SourceCodeIntegrationEditor.ViewModels
{
    [AutoRegister]
    public class TrinityStringsViewModel : WizardStyleViewModelBase, IWizard
    {
        private readonly ITrinityStringsSourceParser trinityStringsSourceParser;
        private readonly IDatabaseProvider databaseProvider;
        private readonly ITrinityStringSqlGenerator trinityStringSqlGenerator;
        public string Title => "Trinity strings wizard";
        public ImageUri? Icon => new ImageUri("icons/document_trinity_strings.png");

        public ObservableCollection<TrinityStringViewModel> TrinityStringsNoDatabase { get; } = new();
        public ObservableCollection<TrinityStringViewModel> ChosenStrings { get; } = new();
        
        public TrinityStringsViewModel(IMessageBoxService messageBoxService,
            ICoreSourceSettings coreSourceSettings,
            ISourceSqlUpdateService sqlUpdateService,
            IAuthMySqlExecutor authExecutor,
            IMySqlExecutor worldExecutor,
            IWindowManager windowManager, 
            ITaskRunner taskRunner,
            IStatusBar statusBar,
            INativeTextDocument resultCode,
            ITrinityStringsSourceParser trinityStringsSourceParser,
            IDatabaseProvider databaseProvider,
            ITrinityStringSqlGenerator trinityStringSqlGenerator
            ) : base(messageBoxService, coreSourceSettings, sqlUpdateService, authExecutor, worldExecutor, windowManager, taskRunner, statusBar, resultCode)
        {
            this.trinityStringsSourceParser = trinityStringsSourceParser;
            this.databaseProvider = databaseProvider;
            this.trinityStringSqlGenerator = trinityStringSqlGenerator;
        }

        protected override int TotalSteps => 4;
        
        protected override void OnChangeStep(uint old, uint nnew)
        {
            if (old == 0 && nnew == 1)
            {
                Load().ListenErrors();
            }
            else if (old == 1 && nnew == 2)
            {
                ChosenStrings.Clear();
                ChosenStrings.AddRange(TrinityStringsNoDatabase.Where(t => t.IsSelected));
            }
            base.OnChangeStep(old, nnew);
        }

        protected override (string? auth, string? world) GenerateSql() => (null, trinityStringSqlGenerator.GenerateWorld(ChosenStrings));
        protected override string SqlSuffixName => "strings";

        private async Task Load()
        {
            IsLoading = true;
            var enums = trinityStringsSourceParser.ParseTrinityStringsEnum();

            var definedList = await databaseProvider.GetStringsAsync();

            var defined = new HashSet<uint>(definedList.Select(d => d.Entry));
            
            TrinityStringsNoDatabase.Clear();
            List<TrinityStringViewModel> temp = new();
            foreach (var e in enums)
            {
                if (defined.Contains(e.Key))
                    continue;
                
                temp.Add(new TrinityStringViewModel(e.Key, e.Value));
            }

            temp.Reverse();
            TrinityStringsNoDatabase.AddRange(temp);
            
            IsLoading = false;
        }
    }
}