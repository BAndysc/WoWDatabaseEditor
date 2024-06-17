using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AvaloniaStyles.Controls.FastTableView;
using DynamicData.Binding;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Common.Utils.DragDrop;
using WDE.DatabaseEditors.Data.Structs;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WDE.DatabaseDefinitionEditor.ViewModels.DefinitionEditor;

public partial class DefinitionViewModel : ObservableBase, IDropTarget
{
    private readonly DefinitionEditorViewModel parent;
    private readonly IVersionedFilesService versionedFilesService;
    public DefinitionStubViewModel DefinitionStub { get; }

    [Notify] private IReadOnlyList<string> compatibility = System.Array.Empty<string>();
    [Notify] private string name = "";
    [Notify] private string singleSolutionName = "";
    [Notify] private string multiSolutionName = "";
    [Notify] private string description = "";
    [Notify] private string tableName = "";
    [Notify] private string tablePrimaryKeyColumnName = "";
    [Notify] [AlsoNotify(nameof(IsTemplate), nameof(IsMultiRecord), nameof(IsSingleRow))] private RecordMode recordMode;
    [Notify] private DataDatabaseType dataDatabaseType;
    [Notify] private OnlyConditionMode isOnlyConditionsTable;
    [Notify] private bool skipQuickLoad;
    [Notify] private string? groupName;
    [Notify] private string? reloadCommand;
    [Notify] private ColumnValueTypeViewModel? picker;
    [Notify] private string? tableNameSource;
    [Notify] private string? groupByKey = null;
    [Notify] private bool hasCondition;
    [Notify] private ConditionReferenceViewModel? condition;
    [Notify] private string? autofillBuildColumn = null;
    [Notify] private GuidType? autoKeyValue;
    
    public FileHandle SourceFile { get; }
    
    public ObservableCollectionExtended<ForeignTableViewModel> ForeignTables { get; } = new ObservableCollectionExtended<ForeignTableViewModel>();
    [Notify] private ForeignTableViewModel? selectedForeignTable;
    
    public ObservableCollectionExtended<SortByViewModel> SortBy { get; } = new ObservableCollectionExtended<SortByViewModel>();
    [Notify] private SortByViewModel? selectedSortBy;

    [Notify] private bool hasCustomPrimaryKeyOrder;
    public ObservableCollectionExtended<CustomPrimaryKeyViewModel> CustomPrimaryKey { get; } = new ObservableCollectionExtended<CustomPrimaryKeyViewModel>();
    [Notify] private CustomPrimaryKeyViewModel? selectedCustomPrimaryKey;

    public ObservableCollectionExtended<CommandViewModel> Commands { get; } =new ObservableCollectionExtended<CommandViewModel>();
    [Notify] private CommandViewModel? selectedCommand;
    
    [Notify] [AlsoNotify(nameof(ColumnsPreview), nameof(CellsPreview))] private List<ColumnViewModel> columns = new List<ColumnViewModel>();
    public IReadOnlyList<ITableColumnHeader> ColumnsPreview => columns;
    public IReadOnlyList<ITableCell> CellsPreview => columns;
    
    [Notify] [AlsoNotify(nameof(SelectedColumn), nameof(SelectedGroup))] private object? selectedColumnOrGroup;

    public ColumnViewModel? SelectedColumn => selectedColumnOrGroup as ColumnViewModel;
    
    public ColumnGroupViewModel? SelectedGroup => selectedColumnOrGroup as ColumnGroupViewModel;
    
    public ObservableCollectionExtended<ColumnGroupViewModel> Groups { get; } = new();
    
    [Notify] private string? iconPath;

    public ObservableCollectionExtended<DatabaseSourceColumnViewModel> DatabaseSourceColumns { get; } = new();

    public ObservableCollectionExtended<ColumnValueTypeViewModel> AllParameters { get; }

    public DefinitionEditorViewModel Parent => parent;
    
    public ICommand AddSortBy { get; }
    public ICommand DeleteSelectedSortBy { get; }
    
    public ICommand AddCommand { get; }
    public ICommand DeleteSelectedCommand { get; }
    
    public ICommand AddCustomPrimaryKey { get; }
    public ICommand DeleteSelectedPrimaryKey { get; }
    
    public ICommand AddForeignTable { get; }
    public ICommand DeleteSelectedForeignTable { get; }
    
    public ICommand AddGroupCommand { get; }
    public ICommand AddColumnCommand { get; }
    public ICommand DeleteSelectedColumnOrGroup { get; }
    public AsyncAutoCommand<ForeignTableViewModel> ImportForeignTableCommand { get; }
    public AsyncAutoCommand ImportMissingColumnsCommand { get; }
    
    public event Action? OnDataChanged;
    
    public DefinitionViewModel(IVersionedFilesService versionedFilesService,
        DefinitionEditorViewModel parent,
        DatabaseTableDefinitionJson model,
        DefinitionStubViewModel stub)
    {
        this.parent = parent;
        this.versionedFilesService = versionedFilesService;
        DefinitionStub = stub;
        SourceFile = versionedFilesService.OpenFile(new FileInfo(model.AbsoluteFileName));
        AllParameters = parent.AllParameters;
        Compatibility = model.Compatibility;
        name = model.Name;
        singleSolutionName = model.SingleSolutionName;
        multiSolutionName = model.MultiSolutionName;
        description = model.Description;
        tableName = model.TableName;
        if (model.TablePrimaryKeyColumnName.HasValue && model.TablePrimaryKeyColumnName.Value.ForeignTable != null)
            throw new Exception("Error: TablePrimaryKeyColumnName cannot have ForeignTable set!");
        tablePrimaryKeyColumnName = model.TablePrimaryKeyColumnName?.ColumnName ?? "";
        recordMode = model.RecordMode;
        dataDatabaseType = model.DataDatabaseType;
        isOnlyConditionsTable = model.IsOnlyConditionsTable;
        skipQuickLoad = model.SkipQuickLoad;
        groupName = model.GroupName;
        iconPath = model.IconPath;
        reloadCommand = model.ReloadCommand;
        if (model.SortBy != null)
        {
            SortBy.AddRange(model.SortBy.Select(col =>
            {
                if (col.ForeignTable != null)
                    throw new Exception("Non null foreign table in SortBy column! This could be supported if there is a need for it.");
                return new SortByViewModel(this, col.ColumnName);
            }));
        }
        picker = string.IsNullOrEmpty(model.Picker)
            ? null
            : AllParameters.FirstOrDefault(x => x.ParameterName == model.Picker) ?? ColumnValueTypeViewModel.FromValueType(model.Picker, true);
        tableNameSource = model.TableNameSource;
        if (model.Commands != null)
            Commands.AddRange(model.Commands.Select(c => new CommandViewModel(this, c)));
        
        groupByKey = model.GroupByKey;
        hasCondition = model.Condition != null;
        condition = model.Condition == null ? null : new ConditionReferenceViewModel(this, model.Condition);
        if (model.ForeignTable != null)
        {
            foreach (var foreignTable in model.ForeignTable)
                ForeignTables.Add(new ForeignTableViewModel(this, foreignTable));
            if (ForeignTables.Count > 0)
                SelectedForeignTable = ForeignTables[0];
        }
        
        autofillBuildColumn = model.AutofillBuildColumn;
        autoKeyValue = model.AutoKeyValue;

        if (model.PrimaryKey != null)
           CustomPrimaryKey.AddRange(model.PrimaryKey.Select(x =>
           {
               if (x.ForeignTable != null)
                   throw new Exception("Error: CustomPrimaryKey cannot have ForeignTable set!");
               return new CustomPrimaryKeyViewModel(this, x.ColumnName);
           }));
        
        On(() => TablePrimaryKeyColumnName, _ => RebuildColumnPreview());
        On(() => RecordMode, _ => RebuildColumnPreview());
        On(() => HasCondition, has =>
        {
            if (has && Condition == null)
                Condition = new ConditionReferenceViewModel(this, new DatabaseConditionReferenceJson());
        });
        On(() => DataDatabaseType, _ => LoadDatabaseColumns().ListenErrors());
        On(() => TableName, _ => LoadDatabaseColumns().ListenErrors());
        
        this.ToObservable(o => o.SelectedColumn)
            .SubscribeAction(col =>
            {
                if (col == null)
                    return;
                var group = Groups.FirstOrDefault(g => g.Columns.Any(c => c == col));
                if (group != null)
                    group.IsExpanded = true;
            });
        
        ForeignTables.CollectionChanged += (_, _) =>
        {
            LoadDatabaseColumns().ListenErrors();
        };
        
        AddSortBy = new DelegateCommand(() =>
        {
            SortBy.Add(new SortByViewModel(this, "-"));
            SelectedSortBy = SortBy[^1];
        });
        DeleteSelectedSortBy = new DelegateCommand(() =>
        {
            if (SelectedSortBy != null)
                SortBy.Remove(SelectedSortBy);
        });
        
        AddCommand = new DelegateCommand(() =>
        {
            Commands.Add(new CommandViewModel(this, new DatabaseCommandDefinitionJson()));
            SelectedCommand = Commands[^1];
        });
        DeleteSelectedCommand = new DelegateCommand(() =>
        {
            if (selectedCommand != null)
                Commands.Remove(selectedCommand);
        });
        
        AddCustomPrimaryKey = new DelegateCommand(() =>
        {
            CustomPrimaryKey.Add(new CustomPrimaryKeyViewModel(this, "-"));
            SelectedCustomPrimaryKey = CustomPrimaryKey[^1];
        });
        DeleteSelectedPrimaryKey = new DelegateCommand(() =>
        {
            if (SelectedCustomPrimaryKey != null)
                CustomPrimaryKey.Remove(SelectedCustomPrimaryKey);
        });
        
        AddForeignTable = new DelegateCommand(() =>
        {
            ForeignTables.Add(new ForeignTableViewModel(this, new DatabaseForeignTableJson()));
            SelectedForeignTable = ForeignTables[^1];
        });

        AddGroupCommand = new DelegateCommand(() =>
        {
            var index = Groups.Count;
            if (SelectedGroup != null)
                index = Groups.IndexOf(SelectedGroup) + 1;
            else if (SelectedColumn != null)
                index = Groups.IndexOf(SelectedColumn.Parent) + 1;
            SelectedColumnOrGroup = AddGroup(new ColumnGroupViewModel(this, new DatabaseColumnsGroupJson()
            {
                Name = "new group"
            }), index);
        });
        
        AddColumnCommand = new DelegateCommand(() =>
        {
            ColumnGroupViewModel group;
            var index = 0;
            if (Groups.Count == 0)
                AddGroupCommand.Execute(null);

            if (SelectedGroup != null)
            {
                group = SelectedGroup;
                index = group.Columns.Count;
            }
            else if (SelectedColumn != null)
            {
                group = SelectedColumn.Parent;
                index = group.Columns.IndexOf(SelectedColumn) + 1;
            }
            else
            {
                group = Groups[^1];
                index = group.Columns.Count;
            }

            var vm = CreateColumn(group, stub.Definition, new DatabaseColumnJson()
            {
                Name = "new column"
            });
            group.Columns.Insert(index, vm);
            SelectedColumnOrGroup = vm;
            RebuildColumnPreview();
        });

        DeleteSelectedColumnOrGroup = new DelegateCommand(() =>
        {
            if (SelectedGroup != null)
            {
                var indexOf = Groups.IndexOf(SelectedGroup);
                if (Groups.Remove(SelectedGroup))
                    SelectedColumnOrGroup = Groups.Count > indexOf ? Groups[indexOf] : null;
            }
            else if (SelectedColumn != null)
            {
                var indexOf = SelectedColumn.Parent.Columns.IndexOf(SelectedColumn);
                var parent = SelectedColumn.Parent;
                if (parent.Columns.Remove(SelectedColumn))
                    SelectedColumnOrGroup = parent.Columns.Count > indexOf ? parent.Columns[indexOf] : null;
            }
            RebuildColumnPreview();
        });
        
        DeleteSelectedForeignTable = new DelegateCommand(() =>
        {
            if (SelectedForeignTable != null)
                ForeignTables.Remove(SelectedForeignTable);
        });

        ImportMissingColumnsCommand = new AsyncAutoCommand(async () =>
        {
            var definition = await parent.GeneratorService.GenerateDefinition(new DatabaseTable(dataDatabaseType, TableName));
            ColumnGroupViewModel? groupVm = null;

            var existing = Groups.SelectMany(g => g.Columns)
                .Where(c => c.DatabaseColumnName != null && !c.DatabaseColumnName.IsForeignTable)
                .Select(c => c.DatabaseColumnName!.ColumnName)
                .ToHashSet();
            
            foreach (var group in definition.Groups)
            {
                foreach (var column in group.Fields)
                {
                    if (existing.Contains(column.DbColumnName))
                        continue;

                    if (groupVm == null)
                    {
                        groupVm = AddGroup(new ColumnGroupViewModel(this, new DatabaseColumnsGroupJson()
                        {
                            Name = "(missing)"
                        }));
                    }
                    
                    var columnVm = CreateColumn(groupVm, null, column);
                    groupVm.Columns.Add(columnVm);
                }
            }
            
            if (groupVm != null)
                RebuildColumnPreview();
        });
        
        ImportForeignTableCommand = new AsyncAutoCommand<ForeignTableViewModel>(async foreignTable =>
        {
            var definition = await parent.GeneratorService.GenerateDefinition(new DatabaseTable(dataDatabaseType, foreignTable.TableName));

            var groupVm = AddGroup(new ColumnGroupViewModel(this, new DatabaseColumnsGroupJson()
            {
                Name = foreignTable.TableName + " (FK)"
            }));
            
            foreach (var group in definition.Groups)
            {
                foreach (var column in group.Fields)
                {
                    if (foreignTable.ForeignKeys.Any(k => column.DbColumnName == k.ColumnName))
                        continue;

                    column.ForeignTable = foreignTable.TableName;
                    var columnVm = CreateColumn(groupVm, null, column);
                    groupVm.Columns.Add(columnVm);
                }
            }
            RebuildColumnPreview();
        });
    }

    public bool IsTemplate => recordMode == RecordMode.Template;
    public bool IsSingleRow => recordMode == RecordMode.SingleRow;
    public bool IsMultiRecord => recordMode == RecordMode.MultiRecord;

    public ColumnGroupViewModel AddGroup(ColumnGroupViewModel groupViewModel, int? index = null)
    {
        if (Groups.Count == 0)
            groupViewModel.IsExpanded = true; // let's expand the first group
        if (index.HasValue)
            Groups.Insert(index.Value, groupViewModel);
        else
            Groups.Add(groupViewModel);
        RebuildColumnPreview();
        RaisePropertyChanged(nameof(Groups));
        return groupViewModel;
    }

    private void RebuildColumnPreview()
    {
        var flat = new List<ColumnViewModel>();
        foreach (var group in Groups)
        {
            foreach (var column in group.Columns)
            {
                if (recordMode == RecordMode.MultiRecord &&
                    tablePrimaryKeyColumnName != null &&
                    column.DatabaseColumnName?.ColumnName == tablePrimaryKeyColumnName)
                    continue;
                flat.Add(column);                
            }
        }
        Columns = flat;
    }

    public ColumnViewModel CreateColumn(ColumnGroupViewModel parent, DatabaseTableDefinitionJson? tableDefinition, DatabaseColumnJson definition)
    {
        var vm = new ColumnViewModel(parent, definition);
        if (vm.IsDatabaseColumnType)
        {
            vm.DatabaseColumnName = DatabaseSourceColumns.FirstOrDefault(c => string.Equals(c.ColumnName, definition.DbColumnName, StringComparison.CurrentCultureIgnoreCase) &&
                                                                              ((definition.ForeignTable == null && !c.IsForeignTable) ||
                                                                              (definition.ForeignTable != null && c.TableName == definition.ForeignTable)))
                ?? new DatabaseSourceColumnViewModel(definition.DbColumnName, TableName, false, "unknown", false);
            vm.ValueType = AllParameters.FirstOrDefault(p => p.Name == definition.ValueType);
            
            if (vm.ValueType == null)
            {
                if (this.parent.ParameterFactory.Factory(definition.ValueType) is { } longP)
                    vm.ValueType = ColumnValueTypeViewModel.FromParameter(longP, definition.ValueType);
                else if (this.parent.ParameterFactory.FactoryString(definition.ValueType) is { } stringP)
                    vm.ValueType = ColumnValueTypeViewModel.FromParameter(stringP, definition.ValueType);
                else
                    vm.ValueType = ColumnValueTypeViewModel.FromValueType(definition.ValueType, false);
            }
            else
            {
                if (!vm.IsZeroBlank && 
                    vm.ValueType.IsParameter && vm.ValueType.Parameter is IParameter<long> lParam &&
                    lParam.Items != null &&
                    lParam.Items.Count > 0)
                {
                    vm.StringValue = lParam.Items.Keys.First().ToString();
                }
            }

            vm.IsPrimaryKey = tableDefinition?.PrimaryKey?.Contains(definition.DbColumnFullName) ?? false;
        }
        // that will cause the very fast tree view to be refresehed
        vm.ToObservable().SubscribeAction(_ => OnDataChanged?.Invoke());
        return vm;
    }

    private CancellationTokenSource? loadDatabaseColumnsCts;

    public async Task LoadDatabaseColumns()
    {
        if (loadDatabaseColumnsCts != null)
            loadDatabaseColumnsCts.Cancel();
        loadDatabaseColumnsCts = new CancellationTokenSource();
        await LoadDatabaseColumnsImpl(loadDatabaseColumnsCts.Token);    
    }
    
    private async Task LoadDatabaseColumnsImpl(CancellationToken cancel)
    {
        DatabaseSourceColumns.Clear();
        
        async Task LoadTableName(string table)
        {
            // all columns
            var dbColumns = await parent.QueryExecutor.GetTableColumns(dataDatabaseType, table);
            if (cancel.IsCancellationRequested)
                return;
        
            DatabaseSourceColumns.AddRange(dbColumns.Select(c => new DatabaseSourceColumnViewModel(c.ColumnName, table, table != tableName, c.DatabaseType, c.Nullable)));            
        }

        string[] tableNames = new[] { tableName }.Concat(ForeignTables.Select(ft => ft.TableName)).ToArray();

        foreach (var table in tableNames)
        {
            await LoadTableName(table);
            if (cancel.IsCancellationRequested)
                return;
        }
    }
    
    public void DragOver(IDropInfo dropInfo)
    {
        if (dropInfo.Data is ColumnGroupViewModel group)
        {
            ColumnGroupViewModel? targetItem = dropInfo.TargetItem as ColumnGroupViewModel;

            if (targetItem != null)
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                dropInfo.Effects = DragDropEffects.Move;
            }
        }
        else if (dropInfo.Data is ColumnViewModel column)
        {
            ColumnGroupViewModel? targetItem = dropInfo.TargetItem as ColumnGroupViewModel;
            ColumnViewModel? targetColumnItem = dropInfo.TargetItem as ColumnViewModel;

            if (targetItem != null)
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                dropInfo.InsertPosition = RelativeInsertPosition.TargetItemCenter;
                dropInfo.Effects = DragDropEffects.Move;
            }
            else
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                dropInfo.Effects = DragDropEffects.Move;
            }
        }
        else
        {
            dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
            dropInfo.Effects = DragDropEffects.Move;
        }
    }

    public void Drop(IDropInfo dropInfo)
    {
        if (dropInfo.Data is SortByViewModel data)
        {
            int indexOf = SortBy.IndexOf(data);
            int dropIndex = dropInfo.InsertIndex;
            if (indexOf < dropIndex)
                dropIndex--;

            SortBy.RemoveAt(indexOf);
            SortBy.Insert(dropIndex, data);
            SelectedSortBy = data;
        }
        else if (dropInfo.Data is CustomPrimaryKeyViewModel pk)
        {
            int indexOf = CustomPrimaryKey.IndexOf(pk);
            int dropIndex = dropInfo.InsertIndex;
            if (indexOf < dropIndex)
                dropIndex--;

            CustomPrimaryKey.RemoveAt(indexOf);
            CustomPrimaryKey.Insert(dropIndex, pk);
            SelectedCustomPrimaryKey = pk;
        }
        else if (dropInfo.Data is CommandViewModel cmd)
        {
            int indexOf = Commands.IndexOf(cmd);
            int dropIndex = dropInfo.InsertIndex;
            if (indexOf < dropIndex)
                dropIndex--;

            Commands.RemoveAt(indexOf);
            Commands.Insert(dropIndex, cmd);
            SelectedCommand = cmd;
        }
        else if (dropInfo.Data is ForeignTableViewModel foreignTable)
        {
            int indexOf = ForeignTables.IndexOf(foreignTable);
            int dropIndex = dropInfo.InsertIndex;
            if (indexOf < dropIndex)
                dropIndex--;

            ForeignTables.RemoveAt(indexOf);
            ForeignTables.Insert(dropIndex, foreignTable);
            SelectedForeignTable = foreignTable;
        }
        else if (dropInfo.Data is ColumnGroupViewModel group)
        {
            int indexOf = Groups.IndexOf(group);
            int dropIndex = dropInfo.InsertIndex;
            // why is that not needed for ListBox handlers? hmm
            if (dropInfo.InsertPosition == RelativeInsertPosition.AfterTargetItem)
                dropIndex++;
            if (indexOf < dropIndex)
                dropIndex--;

            Groups.RemoveAt(indexOf);
            Groups.Insert(dropIndex, group);
            RebuildColumnPreview();
            SelectedColumnOrGroup = group;
        }
        else if (dropInfo.Data is ColumnViewModel column)
        {
            var targetGroup = dropInfo.TargetItem as ColumnGroupViewModel;
            var targetColumn = dropInfo.TargetItem as ColumnViewModel;
            if (targetGroup != null)
            {
                Debug.Assert(dropInfo.DropTargetAdorner == DropTargetAdorners.Highlight);
                column.Parent.Columns.Remove(column);
                targetGroup.Columns.Add(column);
                column.Parent = targetGroup;
            }
            else if (targetColumn != null)
            {
                int dropIndex = dropInfo.InsertIndex;
                
                if (dropInfo.InsertPosition == RelativeInsertPosition.AfterTargetItem)
                    dropIndex++;
                
                var commonParent = targetColumn.Parent == column.Parent;
                if (commonParent)
                {
                    int indexOf = column.Parent.Columns.IndexOf(column);
                    if (indexOf < dropIndex)
                        dropIndex--;
                }
                
                column.Parent.Columns.Remove(column);
                targetColumn.Parent.Columns.Insert(dropIndex, column);
                column.Parent = targetColumn.Parent;
            }
            SelectedColumnOrGroup = column;
            RebuildColumnPreview();
        }
    }
    
    public partial class SortByViewModel : ObservableBase
    {
        public DefinitionViewModel Parent { get; }
        [Notify] private string columnName;

        public SortByViewModel(DefinitionViewModel parent, string columnName)
        {
            Parent = parent;
            this.columnName = columnName;
        }
    }

    
    public partial class CustomPrimaryKeyViewModel : ObservableBase
    {
        public DefinitionViewModel Parent { get; }
        [Notify] private string columnName;

        public CustomPrimaryKeyViewModel(DefinitionViewModel parent, string columnName)
        {
            Parent = parent;
            this.columnName = columnName;
        }
    }

    public void UpdateHasCustomPrimaryKey()
    {
        var order = CustomPrimaryKey
                .Select(col => Groups.SelectMany(g => g.Columns)
                .ToList()
                .IndexIf(c =>
                c.DatabaseColumnName != null && c.DatabaseColumnName.ColumnName == col.ColumnName &&
                c.DatabaseColumnName.IsForeignTable == false))
            .ToList();
        HasCustomPrimaryKeyOrder = !order.SequenceEqual(order.OrderBy(x => x));
    }
}