using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using DynamicData;
using Prism.Commands;
using Prism.Events;
using WDE.Common;
using WDE.Common.Events;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.History;
using WDE.DatabaseEditors.Loaders;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.QueryGenerators;
using WDE.DatabaseEditors.Solution;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.Parameters.Models;

namespace WDE.DatabaseEditors.ViewModels
{
    public class TemplateDbTableEditorViewModel : ObservableBase, ISolutionItemDocument
    {
        private readonly IItemFromListProvider itemFromListProvider;
        private readonly IMessageBoxService messageBoxService;
        private readonly IParameterFactory parameterFactory;

        private readonly DatabaseTableSolutionItem solutionItem;
        private readonly IDatabaseTableDataProvider tableDataProvider;

        private bool Collect(System.IObservable<bool> b)
        {
            bool report = true;
            b.SubscribeAction(val => report = val).Dispose();
            return report;
        }
        
        public TemplateDbTableEditorViewModel(DatabaseTableSolutionItem solutionItem,
            IDatabaseTableDataProvider tableDataProvider, IItemFromListProvider itemFromListProvider,
            IHistoryManager history, ITaskRunner taskRunner, IMessageBoxService messageBoxService,
            IEventAggregator eventAggregator, ITableDefinitionProvider definitionProvider,
            IParameterFactory parameterFactory,
            IQueryGenerator queryGenerator)
        {
            SolutionItem = solutionItem;
            this.itemFromListProvider = itemFromListProvider;
            this.solutionItem = solutionItem;
            this.tableDataProvider = tableDataProvider;
            this.messageBoxService = messageBoxService;
            this.parameterFactory = parameterFactory;
            History = history;
            tableData = null!;
            
            IsLoading = true;
            taskRunner.ScheduleTask($"Loading {solutionItem.TableId}..", LoadTableDefinition);
            
            Title = $"{solutionItem.TableId} Editor";

            undoCommand = new DelegateCommand(History.Undo, () => History.CanUndo);
            redoCommand = new DelegateCommand(History.Redo, () => History.CanRedo);
            OpenParameterWindow = new AsyncAutoCommand<ParameterValue<long>?>(EditParameter);
            saveModifiedFields = new DelegateCommand(SaveSolutionItem);

            TableDefinition = definitionProvider.GetDefinition(solutionItem.TableId)!;
            
            CurrentFilter = FunctionalExtensions.Select<string, Func<DatabaseCellViewModel, bool>>(this.ToObservable(t => t.SearchText), text =>
                {
                    if (string.IsNullOrEmpty(text))
                        return item => true;
                    var lower = text.ToLower();
                    return item => item.FieldName.ToLower().Contains(lower);
                });


            // AutoDispose(eventAggregator.GetEvent<EventRequestGenerateSql>()
            //     .Subscribe(args =>
            //     {
            //         if (args.Item is DatabaseTableSolutionItem dbEditItem)
            //         {
            //             if (solutionItem.Equals(dbEditItem))
            //             {
            //                 args.Sql = queryGenerator.GenerateQuery(tableData, solutionItem.TableId, solutionItem.Entry,
            //                     GetModifiedFields());
            //             }
            //         }
            //     }));
        }
       
        private async Task EditParameter(ParameterValue<long>? valueHolder)
        {
            if (valueHolder == null)
                return;
            
            if (valueHolder.Parameter.HasItems)
            {
                var result = await itemFromListProvider.GetItemFromList(valueHolder.Parameter.Items,
                    valueHolder.Parameter is FlagParameter, valueHolder.Value);
                if (result.HasValue)
                    valueHolder.Value = result.Value;
            }
        }

        private async Task LoadTableDefinition()
        {
            var data = await tableDataProvider.Load(solutionItem.TableId, solutionItem.Entries.ToArray()) as DatabaseTableData;

            if (data == null)
            {
                await messageBoxService.ShowDialog(new MessageBoxFactory<bool>().SetTitle("Error!")
                    .SetMainInstruction($"Editor failed to load data from database!")
                    .SetIcon(MessageBoxIcon.Error)
                    .WithOkButton(true)
                    .Build());
                return;
            }
            //
            // if (solutionItem.ModifiedFields != null)
            // {
            //     foreach (var field in data.Categories.SelectMany(c => c.Fields))
            //     {
            //         if (!solutionItem.ModifiedFields.TryGetValue(field.FieldMetaData.DbColumnName, out var state))
            //             continue;
            //         
            //         if (field is IStateRestorableField restorableField)
            //             restorableField.RestoreLoadedFieldState(state);
            //     }
            // }

            TableData = data;
            SetupHistory();
            IsLoading = false;
        }

        // private Dictionary<string, DatabaseSolutionItemModifiedField> GetModifiedFields()
        // {
        //     var dict = new Dictionary<string, DatabaseSolutionItemModifiedField>();
        //     
        //     foreach (var field in tableData.Categories.SelectMany(c => c.Fields).Where(f => f.IsModified))
        //     {
        //         if (field is IStateRestorableField restorableField)
        //             dict[field.FieldMetaData.DbColumnName] = new(field.FieldMetaData.DbColumnName, 
        //                 restorableField.GetOriginalValueForPersistence(), 
        //                 restorableField.GetValueForPersistence());
        //     }
        //
        //     return dict;
        // }

        private void SaveSolutionItem()
        {
            //solutionItem.ModifiedFields = GetModifiedFields();
            History.MarkAsSaved();
        }
        
        private void SetupHistory()
        {
            var historyHandler = AutoDispose(new TemplateTableEditorHistoryHandler(tableData));
            History.PropertyChanged += (sender, args) =>
            {
                undoCommand.RaiseCanExecuteChanged();
                redoCommand.RaiseCanExecuteChanged();
                IsModified = !History.IsSaved;
            };
            History.AddHandler(historyHandler);
        }

        public IObservable<Func<DatabaseCellViewModel, bool>> CurrentFilter { get; }
        public ObservableCollection<DatabaseEntityViewModel> Rows { get; } = new();

        public DatabaseTableDefinitionJson TableDefinition { get; }
        private DatabaseTableData tableData;
        public DatabaseTableData TableData
        {
            get => tableData;
            set
            {
                foreach (var group in TableDefinition.Groups)
                {
                    groupVisibilityByName.Add(group.Name, new ReactiveProperty<bool>(true));
                }
                
                tableData = value;
                RaisePropertyChanged(nameof(TableData));
                var flatFields = tableData.Rows.Select(row => new DatabaseEntityViewModel(parameterFactory, this, row));
                Rows.AddRange(flatFields);
                IObservable<Unit> onChange = Rows[0].Observable;
                for (var index = 1; index < Rows.Count; index++)
                {
                    onChange = Observable.Merge(Rows[index].Observable);
                }

                onChange.Subscribe(_ =>
                {
                    ReEvalVisibility();
                });

            }
        }

        private void ReEvalVisibility()
        {
            foreach (var group in TableDefinition.Groups)
            {
                if (!group.ShowIf.HasValue)
                    continue;

                groupVisibilityByName[group.Name].Value = false;
                foreach (var row in Rows)
                {
                    var cell = row.GetCell(group.ShowIf.Value.ColumnName);
                    if (cell is not DatabaseField<long> lField)
                        continue;
                    if (lField.Parameter.Value == group.ShowIf.Value.Value)
                    {
                        groupVisibilityByName[group.Name].Value = true;
                        break;
                    }
                }
            }
        }

        private Dictionary<string, ReactiveProperty<bool>> groupVisibilityByName = new();

        public System.IObservable<bool> GetGroupVisibility(string str)
        {
            return groupVisibilityByName[str];
        }
        
        private bool isLoading;
        public bool IsLoading
        {
            get => isLoading;
            internal set => SetProperty(ref isLoading, value);
        }
        
        public AsyncAutoCommand<ParameterValue<long>?> OpenParameterWindow { get; }
        private DelegateCommand undoCommand;
        private DelegateCommand redoCommand;
        private readonly DelegateCommand saveModifiedFields;
        
        private string searchText = "";
        public string SearchText
        {
            get => searchText;
            set => SetProperty(ref searchText, value);
        }
        
        public string Title { get; }
        public ICommand Undo => undoCommand;
        public ICommand Redo => redoCommand;
        public ICommand Copy => AlwaysDisabledCommand.Command;
        public ICommand Cut => AlwaysDisabledCommand.Command;
        public ICommand Paste => AlwaysDisabledCommand.Command;
        public ICommand Save => saveModifiedFields;
        public IAsyncCommand? CloseCommand { get; set; } = null;
        public bool CanClose { get; } = true;
        private bool isModified;
        public bool IsModified
        {
            get => isModified;
            private set => SetProperty(ref isModified, value);
        }
        public IHistoryManager History { get; }
        public ISolutionItem SolutionItem { get; }
    }

}