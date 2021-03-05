using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Mvvm;
using Prism.Commands;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Data;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.ViewModels
{
    public class CreatureTemplateDbEditorViewModel: BindableBase, IDocument
    {
        private readonly IDbEditorTableDataProvider tableDataProvider;
        private readonly IWindowManager windowManager;

        // private readonly HistoryHandler historyHandler;
        
        public CreatureTemplateDbEditorViewModel(IDbEditorTableDataProvider tableDataProvider, ITaskRunner taskRunner, IWindowManager windowManager)
        {
            this.tableDataProvider = tableDataProvider;
            this.windowManager = windowManager;
            taskRunner.ScheduleTask("Loading creature...", () => LoadTable());
        }

        private DbTableData? tableData;
        public DbTableData? TableData
        {
            get => tableData;
            set
            {
                tableData = value;
                RaisePropertyChanged(nameof(TableData));
            }
        }

        private async Task LoadTable()
        {
            var td = await tableDataProvider.LoadCreatureTamplateDataEntry(54);
            TableData = td as DbTableData;
        }
        
        public void Dispose()
        {
            
        }

        public string Title { get; } = "Creature Template Editor";
        public ICommand Undo => AlwaysDisabledCommand.Command;
        public ICommand Redo => AlwaysDisabledCommand.Command;
        public ICommand Copy => AlwaysDisabledCommand.Command;
        public ICommand Cut => AlwaysDisabledCommand.Command;
        public ICommand Paste => AlwaysDisabledCommand.Command;
        public ICommand Save => AlwaysDisabledCommand.Command;
        public IAsyncCommand CloseCommand { get; set; } = null;
        public bool CanClose { get; } = true;
        private bool isModified;
        public bool IsModified
        {
            get => isModified;
            private set => SetProperty(ref isModified, value);
        }
        public IHistoryManager History { get; }
    }
}