using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using AvaloniaEdit.Document;
using MySqlConnector;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Services.QueryParser.Models;
using WDE.Common.Tasks;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Common.Utils.DragDrop;
using WDE.MVVM;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Models.DataTypes;
using WDE.SqlWorkbench.Services.Connection;
using WDE.SqlWorkbench.Services.QueryConfirmation;
using IDocument = WDE.Common.Managers.IDocument;

namespace WDE.SqlWorkbench.ViewModels;

internal partial class TableCreatorViewModel : ObservableBase, IDocument, IDropTarget
{
    [Notify] private bool isLoading;
    [Notify] private string tableName;
    [Notify] private string tableComment = "";
    [Notify] private string schemaName;
    [Notify] private EngineViewModel selectedEngine;
    [Notify] private CharsetViewModel selectedCharset;
    [Notify] private CollationViewModel? selectedCollation;
    [Notify] private ColumnViewModel? selectedColumn;
    [Notify] private RowFormatViewModel selectedRowFormat;
    [Notify] private IndexViewModel? selectedIndex;
    [Notify] private int selectedTabIndex = -1;

    private readonly IQueryConfirmationService queryConfirmationService;
    private readonly IConnection connection;
    private readonly SchemaName schema;
    private TableInfo? originalTableInfo;

    public TextDocument QueryDocument { get; } = new();
    
    public ObservableCollection<ColumnViewModel> Columns { get; } = new();
    public ObservableCollection<IndexViewModel> Indexes { get; } = new();
    public ObservableCollection<EngineViewModel> Engines { get; } = new();
    public ObservableCollection<CharsetViewModel> Charsets { get; } = new();
    public static ObservableCollection<DataTypeViewModel> DataTypes { get; } = new();
    public static ObservableCollection<RowFormatViewModel> RowFormats { get; } = new();

    public ICommand AddColumnCommand { get; }
    public ICommand AddColumnsCommand { get; }
    public ICommand RemoveSelectedColumnsCommand { get; }
    public ICommand AddIndexCommand { get; }
    public ICommand RemoveSelectedIndexesCommand { get; }
    public IAsyncCommand<IndexViewModel> EditIndexColumnsCommand { get; }
    
    private List<ColumnViewModel> deletedColumns = new();
    private List<ColumnViewModel> originalPrimaryKey = new();
    private List<IndexViewModel> deletedIndexes = new();
    
    static TableCreatorViewModel()
    {
        RowFormats.AddRange(new RowFormatViewModel[]
        {
            new ("(default)", true),
            new (RowFormat.Dynamic),
            new (RowFormat.Fixed),
            new (RowFormat.Compressed),
            new (RowFormat.Redundant),
            new (RowFormat.Compact),
        });
        DataTypes.AddRange(new MySqlType[]
        {
            MySqlType.Numeric(new NumericDataType(NumericDataTypeKind.Int, false)),
            MySqlType.Numeric(new NumericDataType(NumericDataTypeKind.Int, true)),
            MySqlType.Numeric(new NumericDataType(NumericDataTypeKind.BigInt, false)),
            MySqlType.Numeric(new NumericDataType(NumericDataTypeKind.BigInt, true)),
            MySqlType.Numeric(new NumericDataType(NumericDataTypeKind.MediumInt, false)),
            MySqlType.Numeric(new NumericDataType(NumericDataTypeKind.MediumInt, true)),
            MySqlType.Numeric(new NumericDataType(NumericDataTypeKind.SmallInt, false)),
            MySqlType.Numeric(new NumericDataType(NumericDataTypeKind.SmallInt, true)),
            MySqlType.Numeric(new NumericDataType(NumericDataTypeKind.TinyInt, false)),
            MySqlType.Numeric(new NumericDataType(NumericDataTypeKind.TinyInt, true)),
            MySqlType.Numeric(new NumericDataType(NumericDataTypeKind.Float, false)),
            MySqlType.Numeric(new NumericDataType(NumericDataTypeKind.Double, false)),
            MySqlType.Numeric(new NumericDataType(NumericDataTypeKind.Decimal, false)),
            MySqlType.Numeric(new NumericDataType(NumericDataTypeKind.Bit, false)),
            MySqlType.Text(new TextDataType(TextDataTypeKind.Char, 255)),
            MySqlType.Text(new TextDataType(TextDataTypeKind.VarChar, 255)),
            MySqlType.Text(new TextDataType(TextDataTypeKind.Text, 255)),
            MySqlType.Text(new TextDataType(TextDataTypeKind.LongText)),
            MySqlType.Text(new TextDataType(TextDataTypeKind.MediumText)),
            MySqlType.Text(new TextDataType(TextDataTypeKind.TinyText)),
            MySqlType.Text(new TextDataType(TextDataTypeKind.Binary, 255)),
            MySqlType.Text(new TextDataType(TextDataTypeKind.VarBinary, 255)),
            MySqlType.Text(new TextDataType(TextDataTypeKind.Blob, 255)),
            MySqlType.Text(new TextDataType(TextDataTypeKind.LongBlob)),
            MySqlType.Text(new TextDataType(TextDataTypeKind.MediumBlob)),
            MySqlType.Text(new TextDataType(TextDataTypeKind.TinyBlob)),
            MySqlType.Date(new DateDataType(DateTimeDataTypeKind.Date)),
            MySqlType.Date(new DateDataType(DateTimeDataTypeKind.DateTime)),
            MySqlType.Date(new DateDataType(DateTimeDataTypeKind.Time)),
            MySqlType.Date(new DateDataType(DateTimeDataTypeKind.TimeStamp)),
            MySqlType.Date(new DateDataType(DateTimeDataTypeKind.Year)),
            MySqlType.Spatial(new SpatialDataType(SpatialDataTypeKind.Geometry)),
            MySqlType.Spatial(new SpatialDataType(SpatialDataTypeKind.Point)),
            MySqlType.Spatial(new SpatialDataType(SpatialDataTypeKind.LineString)),
            MySqlType.Spatial(new SpatialDataType(SpatialDataTypeKind.Polygon)),
            MySqlType.Spatial(new SpatialDataType(SpatialDataTypeKind.MultiPoint)),
            MySqlType.Spatial(new SpatialDataType(SpatialDataTypeKind.MultiLineString)),
            MySqlType.Spatial(new SpatialDataType(SpatialDataTypeKind.MultiPolygon)),
            MySqlType.Spatial(new SpatialDataType(SpatialDataTypeKind.GeometryCollection)),
            MySqlType.Json(new JsonDataType())
        }.Select(x => new DataTypeViewModel(x)));
    }
    
    public TableCreatorViewModel(IMainThread mainThread,
        IQueryConfirmationService queryConfirmationService,
        IWindowManager windowManager,
        IConnection connection,
        SchemaName schema,
        string? tableName)
    {
        this.queryConfirmationService = queryConfirmationService;
        this.connection = connection;
        this.schema = schema;
        this.tableName = tableName ?? "";
        this.schemaName = schema.Name;
        
        Engines.Add(new EngineViewModel("(default)", true));
        Charsets.Add(new CharsetViewModel("(default)", true));
        Charsets[0].Collations.Add(new CollationViewModel("(default)", true, true));

        selectedRowFormat = RowFormats[0];
        selectedEngine = Engines[0];
        selectedCharset = Charsets[0];
        selectedCollation = Charsets[0].Collations[0];
        
        Save = new AsyncAutoCommand(async () =>
        {
            UpdateQuery();
            await queryConfirmationService.QueryConfirmationAndExecuteAsync(connection, QueryDocument.Text);
        });
        
        AddColumnCommand = new DelegateCommand(() =>
        {
            var indexOfSelected = Math.Clamp(SelectedColumn == null ? Columns.Count : Columns.IndexOf(SelectedColumn) + 1, 0, Columns.Count);
            Columns.Insert(indexOfSelected, new ColumnViewModel()
            {
                DataType = DataTypes[0],
                ColumnName = "column" + (Columns.Count + 1)
            });
            SelectedColumn = Columns[indexOfSelected];
        });
        
        AddColumnsCommand = new DelegateCommand(() =>
        {
            foreach (var dt in DataTypes)
            {
                Columns.Add(new ColumnViewModel()
                {
                    DataType = dt,
                    ColumnName = "column" + (Columns.Count + 1)
                });
            }
        });
        
        RemoveSelectedColumnsCommand = new DelegateCommand(() =>
        {
            if (SelectedColumn == null) 
                return;
            
            var indexOf = Columns.IndexOf(SelectedColumn);
            if (indexOf == -1)
                return;

            deletedColumns.Add(SelectedColumn);
            Columns.RemoveAt(indexOf);
            indexOf = Math.Min(indexOf, Columns.Count - 1);
            SelectedColumn = indexOf == -1 ? null : Columns[indexOf];
        }, () => SelectedColumn != null).ObservesProperty(() => SelectedColumn);
        
        AddIndexCommand = new DelegateCommand(() =>
        {
            var indexOfSelected = Math.Clamp(SelectedIndex == null ? Indexes.Count : Indexes.IndexOf(SelectedIndex) + 1, 0, Indexes.Count);
            Indexes.Insert(indexOfSelected, new IndexViewModel()
            {
            });
            SelectedIndex = Indexes[indexOfSelected];
        });
        
        RemoveSelectedIndexesCommand = new DelegateCommand(() =>
        {
            if (SelectedIndex == null) 
                return;
            
            var indexOf = Indexes.IndexOf(SelectedIndex);
            if (indexOf == -1)
                return;

            deletedIndexes.Add(SelectedIndex);
            Indexes.RemoveAt(indexOf);
            indexOf = Math.Min(indexOf, Indexes.Count - 1);
            SelectedIndex = indexOf == -1 ? null : Indexes[indexOf];
        }, () => SelectedIndex != null).ObservesProperty(() => SelectedIndex);
        
        EditIndexColumnsCommand = new AsyncAutoCommand<IndexViewModel>(async index =>
        {
            using var vm = new IndexColumnsEditorDialogViewModel(Columns, index);
            if (await windowManager.ShowDialog(vm))
            {
                index.Parts.Clear();
                index.Parts.AddRange(vm.Parts);
            }
        });
        
        On(() => SelectedCharset, charset =>
        {
            if (selectedCollation == null || !charset.Collations.Contains(selectedCollation))
            {
                // very annoying, but without a delay, after we set the selected collation here, the combobox still has the old charset
                // and it will reset SelectedCollation to null, because the collation we just set belongs to a new charset, which combobox doesn't know yet
                mainThread.Delay(() =>
                {
                    // we check again, because the user might have changed the charset in the meantime
                    if (selectedCollation == null || !charset.Collations.Contains(selectedCollation))
                        SelectedCollation = charset.Collations.FirstOrDefault(x => x.IsDefault) ?? charset.Collations[0];
                }, TimeSpan.FromMilliseconds(1));
            }
        });
        
        On(() => SelectedTabIndex, tab =>
        {
            if (tab == 2)
                UpdateQuery();
        });
        
        isLoading = true;
        LoadAsync().ListenErrors();
    }

    private void UpdateQuery()
    {
        if (originalTableInfo == null)
            QueryDocument.Text = GenerateCreateQuery();
        else
            QueryDocument.Text = GenerateAlterQuery();
    }
    
    private string GenerateAlterQuery()
    {
        if (originalTableInfo == null)
            throw new Exception("No original table info");

        var tableInfo = originalTableInfo.Value;
        List<string> alterOptions = new();

        var currentPrimaryKey = Columns.Where(x => x.PrimaryKey).OrderByDescending(x => x.AutoIncrement).ToList();
        bool primaryKeyChanged = !currentPrimaryKey.SequenceEqual(originalPrimaryKey);
        
        foreach (var deleted in deletedColumns)
            alterOptions.Add($"DROP COLUMN `{deleted.ColumnName}`");
        
        foreach (var deleted in deletedIndexes)
            alterOptions.Add($"DROP INDEX `{deleted.IndexName}`");
        
        for (int i = 0; i < Columns.Count; ++i)
        {
            var place = i == 0 ? "FIRST" : $"AFTER `{Columns[i - 1].ColumnName}`";
            if (Columns[i].IsNew)
            {
                var create = GenerateColumnCreate(Columns[i]);
                alterOptions.Add($"ADD COLUMN {create} {place}");
            }
            else
            {
                if (Columns[i].IsModified)
                {
                    var original = Columns[i].OriginalColumnInfo!.Value;
                    alterOptions.Add($"CHANGE COLUMN `{original.Name}` {GenerateColumnCreate(Columns[i])}{(Columns[i].IsPositionChanged ? " " + place : "")}");
                }
            }
        }

        foreach (var index in Indexes)
        {
            if (index.IsModified || index.IsNew)
            {
                if (index.IsModified)
                    alterOptions.Add($"DROP INDEX `{index.OriginalIndexInfo!.Value.KeyName}`");
                alterOptions.Add($"ADD {GenerateIndexCreate(index)}");
            }
        }

        if (primaryKeyChanged)
        {
            if (originalPrimaryKey.Count > 0)
                alterOptions.Add($"DROP PRIMARY KEY");
            if (currentPrimaryKey.Count > 0)
                alterOptions.Add($"ADD PRIMARY KEY ({string.Join(", ", currentPrimaryKey.Select(x => $"`{x.ColumnName}`"))})");
        }
        
        if (selectedEngine.Name != tableInfo.Engine)
            alterOptions.Add($"ENGINE = {selectedEngine.Name}");
        
        var tableCharset = Charsets.FirstOrDefault(x => x.Collations.Any(c => c.Name == tableInfo.Collation));

        if (selectedCharset != tableCharset)
            alterOptions.Add($"CHARSET = {selectedCharset.Name}");

        if (selectedCollation != null && selectedCollation.Name != tableInfo.Collation)
            alterOptions.Add($"COLLATE = {selectedCollation.Name}");
        
        if (selectedRowFormat.Format != tableInfo.RowFormat)
            alterOptions.Add($"ROW_FORMAT = {selectedRowFormat.Name}");

        if (tableComment != (tableInfo.Comment ?? ""))
            alterOptions.Add($"COMMENT = '{MySqlHelper.EscapeString(tableComment)}'");

        if (tableName != tableInfo.Name)
            alterOptions.Add($"RENAME TO `{tableName}`");

        if (alterOptions.Count > 0)
            return $"ALTER TABLE `{tableInfo.Schema}`.`{tableInfo.Name}`\n" + string.Join(",\n", alterOptions);
        
        return "";
    }

    private string GenerateIndexCreate(IndexViewModel index)
    {
        string createIndex = "";
        if (index.Kind != IndexKind.NonUnique)
            createIndex += index.Kind.ToString().ToUpper();
            
        createIndex += " INDEX";
            
        if (index.IndexName != null)
            createIndex += $" `{index.IndexName}`";
            
        if (index.Type != IndexType.Default)
            createIndex += $" USING {index.Type.ToString().ToUpper()}";
            
        createIndex += $" ({string.Join(", ", index.Parts.Select(x => x.ToString()))})";
        return createIndex;
    }
    
    private string GenerateColumnCreate(ColumnViewModel column)
    {
        string columnCreate = $"`{column.ColumnName}` {column.DataType}";
        // charset and collate must always go right after type
        if (column.Charset != null && !column.Charset.IsDefaultPlaceholder)
            columnCreate += $" CHARACTER SET {column.Charset.Name}";
        if (column.Collation != null && !column.Collation.IsDefaultPlaceholder)
            columnCreate += $" COLLATE {column.Collation.Name}";
        columnCreate += column.NotNull ? " NOT NULL" : " NULL";
        if (column.AutoIncrement)
            columnCreate += " AUTO_INCREMENT";
        if (column.DefaultValue != null)
        {
            var defaultValue = column.DefaultValue;
            if (column.DataType.Type is { } type &&
                (type.Kind is MySqlTypeKind.Text || 
                 type.Kind is MySqlTypeKind.Date && !defaultValue.StartsWith("CURRENT_TIMESTAMP", StringComparison.OrdinalIgnoreCase)) &&
                !defaultValue.StartsWith('('))
            {
                defaultValue = $"'{MySqlHelper.EscapeString(defaultValue)}'";
            }
            columnCreate += $" DEFAULT {defaultValue}";
        }
        return columnCreate;
    }
    
    private string GenerateCreateQuery()
    {
        StringBuilder sb = new();
        sb.Append("CREATE TABLE ");
        sb.Append($"`{schemaName}`.`{tableName}`");

        List<string> features = new();
        
        foreach (var column in Columns)
        {
            features.Add(GenerateColumnCreate(column));
        }

        if (Columns.Any(x => x.PrimaryKey))
            features.Add("PRIMARY KEY (" + string.Join(", ", Columns.Where(x => x.PrimaryKey).OrderByDescending(x => x.AutoIncrement).Select(x => $"`{x.ColumnName}`")) + ")");

        foreach (var index in Indexes)
        {
            features.Add(GenerateIndexCreate(index));
        }
        
        sb.AppendLine("(");
        sb.AppendLine(string.Join(",\n", features.Select(x => $"    {x}")));
        sb.AppendLine(")");
        
        if (!selectedEngine.IsDefaultPlaceholder)
            sb.AppendLine($"ENGINE = {selectedEngine.Name}");
        if (!selectedCharset.IsDefaultPlaceholder)
            sb.AppendLine($"CHARSET = {selectedCharset.Name}");
        if (selectedCollation != null && !selectedCollation.IsDefaultPlaceholder)
            sb.AppendLine($"COLLATE = {selectedCollation.Name}");
        if (!selectedRowFormat.IsDefaultPlaceholder)
            sb.AppendLine($"ROW_FORMAT = {selectedRowFormat.Name}");
        
        if (!string.IsNullOrEmpty(tableComment))
            sb.AppendLine($"COMMENT = '{MySqlHelper.EscapeString(tableComment)}'");
        
        return sb.ToString();
    }

    private async Task LoadAsync()
    {
        await using var session = await connection.OpenSessionAsync();
        await LoadEnginesAndCharsetsAsync(session);
        if (!string.IsNullOrEmpty(tableName))
            await LoadExistingTableAsync(session);
        IsLoading = false;
    }
    
    private async Task LoadExistingTableAsync(IMySqlSession session)
    {
        var tableInfo = await session.GetTablesAsync(schema.Name, default, tableName);
        if (tableInfo.Count != 1)
            throw new Exception("Table not found");
        
        var table = tableInfo[0];
        originalTableInfo = table;
        TableComment = table.Comment ?? "";
        SelectedEngine = Engines.FirstOrDefault(x => x.Name == table.Engine) ?? Engines[0];
        SelectedCharset = Charsets.FirstOrDefault(x => x.Collations.Any(c => c.Name == table.Collation)) ?? Charsets[0];
        SelectedCollation = SelectedCharset.Collations.FirstOrDefault(x => x.Name == table.Collation) ?? SelectedCharset.Collations[0];
        SelectedRowFormat = RowFormats.FirstOrDefault(x => x.Format == table.RowFormat) ?? RowFormats[0];
        
        var columns = await session.GetTableColumnsAsync(schema.Name, tableName!, default);
        foreach (var columnDef in columns)
        {
            var vm = new ColumnViewModel(columnDef, Charsets);
            Columns.Add(vm);
        }
        var indexes = await session.GetIndexesAsync(schema.Name, tableName, default);
        foreach (var (name, entries) in indexes)
        {
            if (name == "PRIMARY")
                continue;
            
            var index = new IndexViewModel(Columns, entries);
            
            Indexes.Add(index);
            SelectedIndex = index;
        }

        if (indexes.TryGetValue("PRIMARY", out var primaryKey))
            originalPrimaryKey.AddRange(primaryKey.OrderBy(x => x.SeqInIndex).Select(x => Columns.First(col => col.ColumnName == x.ColumnName)));
    }
    
    private async Task LoadEnginesAndCharsetsAsync(IMySqlSession session)
    {
        var engines = await session.GetEnginesAsync();
        Engines.AddRange(engines.Select(x => new EngineViewModel(x)));
        
        var collations = await session.GetCollationsAsync();
        var charsets = collations.GroupBy(x => x.Charset).ToDictionary(x => x.Key, x => x.ToList());
        
        foreach (var charset in charsets)
        {
            var charsetViewModel = new CharsetViewModel(charset.Key, false);
            Charsets.Add(charsetViewModel);
            foreach (var collation in charset.Value)
                charsetViewModel.Collations.Add(new CollationViewModel(collation.Name, false, collation.IsDefault));
        }
    }
    
    public void DragOver(IDropInfo dropInfo)
    {
        dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
        dropInfo.Effects = DragDropEffects.Move;
    }

    public void Drop(IDropInfo dropInfo)
    {
        if (dropInfo.InsertIndex < 0)
            return;

        List<ColumnViewModel> typed;
        if (dropInfo.Data is IReadOnlyList<object?> data)
            typed = data.Where(d => d != null).Cast<ColumnViewModel>().ToList();
        else if (dropInfo.Data is ColumnViewModel vm)
            typed = new List<ColumnViewModel>() { vm };
        else
            return;

        int dropIndex = dropInfo.InsertIndex;

        DropUtils<ColumnViewModel> dropper = new(Columns, typed, dropIndex);
         
        dropper.DoDrop(Columns);
        foreach (var x in typed)
            x.IsPositionChanged = true;
        //SelectedColumn = item;
    }
    
    public ICommand Undo => AlwaysDisabledCommand.Command;
    public ICommand Redo => AlwaysDisabledCommand.Command;
    public IHistoryManager? History => null;
    public bool IsModified => false;
    public string Title => "Table creator";
    public ImageUri? Icon => new ImageUri("Icons/icon_mini_table_edit.png");
    public ICommand Copy => AlwaysDisabledCommand.Command;
    public ICommand Cut => AlwaysDisabledCommand.Command;
    public ICommand Paste => AlwaysDisabledCommand.Command;
    public IAsyncCommand Save { get; }
    public IAsyncCommand? CloseCommand { get; set; }
    public bool CanClose => true;
}