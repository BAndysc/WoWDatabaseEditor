using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using AvaloniaEdit.Document;
using MySqlConnector;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.History;
using WDE.Common.Services.QueryParser.Models;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Models.DataTypes;
using WDE.SqlWorkbench.Services.Connection;
using IDocument = WDE.Common.Managers.IDocument;

namespace WDE.SqlWorkbench.ViewModels;

internal readonly struct SchemaName
{
    public readonly string Name;

    public SchemaName(string name)
    {
        Name = name;
    }
}

internal partial class TableCreatorViewModel : ObservableBase, IDocument
{
    [Notify] private bool isLoading;
    [Notify] private string tableName;
    [Notify] private string tableComment = "";
    [Notify] private string schemaName;
    [Notify] private EngineViewModel selectedEngine;
    [Notify] private CharsetViewModel selectedCharset;
    [Notify] private CollationViewModel? selectedCollation;
    [Notify] private ColumnViewModel? selectedColumn;
    [Notify] private int selectedTabIndex = -1;
    
    private readonly IConnection connection;
    private readonly SchemaName schema;

    public TextDocument QueryDocument { get; } = new();
    
    public ObservableCollection<EngineViewModel> Engines { get; } = new();
    public ObservableCollection<CharsetViewModel> Charsets { get; } = new();
    public ObservableCollection<ColumnViewModel> Columns { get; } = new();
    public ObservableCollection<DataTypeViewModel> DataTypes { get; } = new();

    public ICommand AddColumnsCommand { get; }
    public ICommand RemoveSelectedColumnsCommand { get; }
    
    public TableCreatorViewModel(IMainThread mainThread,
        IConnection connection,
        SchemaName schema,
        string? tableName)
    {
        this.connection = connection;
        this.schema = schema;
        this.tableName = tableName ?? "";
        this.schemaName = schema.Name;
        
        Engines.Add(new EngineViewModel("(default)", true));
        Charsets.Add(new CharsetViewModel("(default)", true));
        Charsets[0].Collations.Add(new CollationViewModel("(default)", true, true));

        DataTypes.AddRange(new MySqlType[]
        {
            MySqlType.Numeric(new NumericDataType(NumericDataTypeKind.Bit)),
            MySqlType.Numeric(new NumericDataType(NumericDataTypeKind.TinyInt)),
            MySqlType.Numeric(new NumericDataType(NumericDataTypeKind.SmallInt)),
            MySqlType.Numeric(new NumericDataType(NumericDataTypeKind.MediumInt)),
            MySqlType.Numeric(new NumericDataType(NumericDataTypeKind.Int)),
            MySqlType.Numeric(new NumericDataType(NumericDataTypeKind.BigInt)),
            MySqlType.Numeric(new NumericDataType(NumericDataTypeKind.Decimal)),
            MySqlType.Numeric(new NumericDataType(NumericDataTypeKind.Float)),
            MySqlType.Numeric(new NumericDataType(NumericDataTypeKind.Double)),
            MySqlType.Text(new TextDataType(TextDataTypeKind.Binary, 255)),
            MySqlType.Text(new TextDataType(TextDataTypeKind.Char, 255)),
            MySqlType.Text(new TextDataType(TextDataTypeKind.VarChar, 255)),
            MySqlType.Text(new TextDataType(TextDataTypeKind.TinyText)),
            MySqlType.Text(new TextDataType(TextDataTypeKind.Text, 255)),
            MySqlType.Text(new TextDataType(TextDataTypeKind.MediumText)),
            MySqlType.Text(new TextDataType(TextDataTypeKind.LongText)),
            MySqlType.Text(new TextDataType(TextDataTypeKind.VarBinary, 255)),
            MySqlType.Text(new TextDataType(TextDataTypeKind.TinyBlob)),
            MySqlType.Text(new TextDataType(TextDataTypeKind.Blob, 255)),
            MySqlType.Text(new TextDataType(TextDataTypeKind.MediumBlob)),
            MySqlType.Text(new TextDataType(TextDataTypeKind.LongBlob)),
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
        
        selectedEngine = Engines[0];
        selectedCharset = Charsets[0];
        selectedCollation = Charsets[0].Collations[0];
        
        Save = new AsyncAutoCommand(async () =>
        {
            
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
                
            Columns.RemoveAt(indexOf);
            indexOf = Math.Min(indexOf, Columns.Count - 1);
            SelectedColumn = indexOf == -1 ? null : Columns[indexOf];
        }, () => SelectedColumn != null).ObservesProperty(() => SelectedColumn);
        
        On(() => SelectedCharset, charset =>
        {
            if (selectedCollation == null || !charset.Collations.Contains(selectedCollation))
            {
                // very annoying, but without a delay, after we set the selected collation here, the combobox still has the old charset
                // and it will reset SelectedCollation to null, because the collation we just set belongs to a new charset, which combobox doesn't know yet
                mainThread.Delay(() =>
                {
                    SelectedCollation = charset.Collations.FirstOrDefault(x => x.IsDefault) ?? charset.Collations[0];
                }, TimeSpan.FromMilliseconds(1));
            }
        });
        
        On(() => SelectedTabIndex, tab =>
        {
            if (tab == 3)
            {
                QueryDocument.Text = GenerateQuery();
            }
        });
        
        isLoading = true;
        LoadAsync().ListenErrors();
    }

    private string GenerateQuery()
    {
        StringBuilder sb = new();
        sb.Append("CREATE TABLE ");
        sb.Append($"`{schemaName}`.`{tableName}`");

        List<string> features = new();
        
        foreach (var column in Columns)
        {
            string columnCreate = $"`{column.ColumnName}` {column.DataType}";
            if (column.NotNull)
                columnCreate += " NOT NULL";
            if (column.AutoIncrement)
                columnCreate += " AUTO_INCREMENT";
            features.Add(columnCreate);
        }

        if (Columns.Any(x => x.PrimaryKey))
            features.Add("PRIMARY KEY (" + string.Join(", ", Columns.Where(x => x.PrimaryKey).Select(x => $"`{x.ColumnName}`")) + ")");

        sb.AppendLine("(");
        sb.AppendLine(string.Join(",\n", features.Select(x => $"    {x}")));
        sb.AppendLine(")");

        
        if (!selectedEngine.IsDefaultPlaceholder)
            sb.AppendLine($"ENGINE = {selectedEngine.Name}");
        if (!selectedCharset.IsDefaultPlaceholder)
            sb.AppendLine($"CHARSET = {selectedCharset.Name}");
        if (selectedCollation != null && !selectedCollation.IsDefaultPlaceholder)
            sb.AppendLine($"COLLATE = {selectedCollation.Name}");
        
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
        TableComment = table.Comment ?? "";
        SelectedEngine = Engines.FirstOrDefault(x => x.Name == table.Engine) ?? Engines[0];
        SelectedCharset = Charsets.FirstOrDefault(x => x.Collations.Any(c => c.Name == table.Collation)) ?? Charsets[0];
        SelectedCollation = SelectedCharset.Collations.FirstOrDefault(x => x.Name == table.Collation) ?? SelectedCharset.Collations[0];
        
        var columns = await session.GetTableColumnsAsync(schema.Name, tableName!, default);
        foreach (var columnDef in columns)
        {
            var vm = new ColumnViewModel();
            vm.ColumnName = columnDef.Name;
            vm.PrimaryKey = columnDef.IsPrimaryKey;
            vm.AutoIncrement = columnDef.IsAutoIncrement;
            vm.NotNull = !columnDef.IsNullable;
            if (!MySqlType.TryParse(columnDef.Type, out var mysqlType))
                throw new Exception("Unknown type " + columnDef.Type);
            vm.DataType = new DataTypeViewModel(mysqlType);
            vm.DefaultValue = columnDef.DefaultValue?.ToString();
            Columns.Add(vm);
        }
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

    public ICommand Undo => AlwaysDisabledCommand.Command;
    public ICommand Redo => AlwaysDisabledCommand.Command;
    public IHistoryManager? History => null;
    public bool IsModified => false;
    public string Title => "Table creator";
    public ICommand Copy => AlwaysDisabledCommand.Command;
    public ICommand Cut => AlwaysDisabledCommand.Command;
    public ICommand Paste => AlwaysDisabledCommand.Command;
    public IAsyncCommand Save { get; }
    public IAsyncCommand? CloseCommand { get; set; }
    public bool CanClose => true;
}

internal partial class ColumnViewModel : ObservableBase
{
    [Notify] private string columnName = "";
    [Notify] private bool primaryKey;
    [Notify] private bool autoIncrement;
    [Notify] private bool notNull;
    [Notify] private DataTypeViewModel dataType = new DataTypeViewModel(null);
    [Notify] private string? defaultValue = null;
}

internal class EngineViewModel
{
    public EngineViewModel(string name, bool isDefaultPlaceholder)
    {
        Name = name;
        IsDefaultPlaceholder = isDefaultPlaceholder;
    }

    public EngineViewModel(TableEngine engine)
    {
        Name = engine.Name;
        IsDefault = engine.IsDefault;
        IsDefaultPlaceholder = false;
        Description = engine.Description;
        SupportsTransactions = engine.SupportsTransactions;
        SupportsXa = engine.SupportsXa;
        SupportsSavePoints = engine.SupportsSavePoints;
    }

    public bool? SupportsSavePoints { get; }
    public bool? SupportsXa { get; }
    public bool? SupportsTransactions { get; }
    public string? Description { get; }
    public string Name { get; }
    public bool IsDefault { get; }
    public bool IsDefaultPlaceholder { get; }
    public override string ToString() => Name;
}

internal class CharsetViewModel
{
    public ObservableCollection<CollationViewModel> Collations { get; } = new();
    
    public CharsetViewModel(string name, bool isDefaultPlaceholder)
    {
        Name = name;
        IsDefaultPlaceholder = isDefaultPlaceholder;
    }

    public string Name { get; }
    public bool IsDefaultPlaceholder { get; }
    public override string ToString() => Name;
}

internal class CollationViewModel
{
    public CollationViewModel(string name, bool isDefaultPlaceholder, bool isDefault)
    {
        Name = name;
        IsDefault = isDefault;
        IsDefaultPlaceholder = isDefaultPlaceholder;
    }

    public string Name { get; }
    public bool IsDefault { get; }
    public bool IsDefaultPlaceholder { get; }
    public override string ToString() => Name;
}

internal class DataTypeViewModel
{
    public DataTypeViewModel(MySqlType? type)
    {
        Type = type;
    }

    public MySqlType? Type { get; }

    public override string ToString()
    {
        return Type?.ToString() ?? "";
    }
}
