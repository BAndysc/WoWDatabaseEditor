using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvaloniaStyles.Controls.FastTableView;
using AvaloniaStyles.Controls.OptimizedVeryFastTableView;
using MySqlConnector;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.Avalonia.Components;
using WDE.Common.Tasks;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Services.ActionsOutput;
using WDE.SqlWorkbench.Services.UserQuestions;
using WDE.SqlWorkbench.Utils;

namespace WDE.SqlWorkbench.ViewModels;

internal partial class SelectResultsViewModel : ObservableBase
{
    private readonly SqlWorkbenchViewModel vm;
    private Vector scrollOffset;

    public Vector ScrollOffset
    {
        get => scrollOffset;
        set
        {
            scrollOffset = value;
            RaisePropertyChanged(nameof(ScrollOffset));
        }
    }
    [Notify] private int selectedRowIndex;
    [Notify] private int selectedCellIndex;
    [Notify] [AlsoNotify(nameof(Count), nameof(IsModified))] private int additionalRowsCount;
    [Notify] private bool requestRender;
    [Notify] private BaseCellEditorViewModel? cellEditor;
    [Notify] private bool showCellEditor;
    private Guid originalConnectionId;
    
    public bool IsModified => alteredRows.Count > 0 || deletedRows.Count > 0 || additionalRowsCount > 0;

    public string TitleWithModifiedStatus => IsModified ? Title + " *" : Title;
    
    public IMultiIndexContainer Selection { get; } = new MultiIndexContainer();
    public string Title { get; protected set; }
    public IReadOnlyList<ITableColumnHeader> Columns { get; private set; } = Array.Empty<ITableColumnHeader>();
    
    public bool ColumnsHeaderAlreadySetAutoSizeWidth { get; set; }
    
    public int Count => Results.AffectedRows + AdditionalRowsCount;
    
    public TableController TableController { get; }

    private SelectResult results;
    public SelectResult Results
    {
        get => results;
        protected set
        {
            results = value;
            var previousWidths = Columns.Select(x => x.Width).ToArray();
            Columns = results.ColumnNames.Select((x, index) => new TableTableColumnHeader(x, index + 1 < previousWidths.Length ? previousWidths[index + 1] : 120)).Prepend(new TableTableColumnHeader("#", previousWidths.Length > 0 ? previousWidths[0] : 50)).ToArray();
            RaisePropertyChanged(nameof(Columns));
            ColumnsOverride = new ISparseColumnData[Results.Columns.Length];
            for (int i = 0; i < Results.Columns.Length; ++i)
            {
                int cellIndex = i + 1; // +1, because 0 is # column
                ColumnsOverride[i] = ISparseColumnData.Create(Results.Columns[i]!.CloneEmpty());
                ColumnsOverride[i].OnRowOverriden += rowIndex =>
                {
                    alteredRows.Add(rowIndex);
                    overridenCells.Add((rowIndex, cellIndex));
                    UpdateView();
                };
            }
            Debug.Assert(ColumnsOverride != null);
            Debug.Assert(AdditionalRowsCount == 0);
        }
    }

    public ISparseColumnData[] ColumnsOverride { get; private set; } = null!;
    
    public virtual ImageUri Icon => new("Icons/icon_mini_view_big.png");
    
    public ICommand CopyColumnNameCommand { get; }
    public ICommand SelectAllCommand { get; }
    public ICommand CopySelectedCommand { get; }
    public ICommand CopyInsertCommand { get; }
    public IAsyncCommand RefreshCommand { get; }
    public IAsyncCommand CreateViewCommand { get; }

    public SelectResultsViewModel(SqlWorkbenchViewModel vm, IActionOutput action, in SelectResult results)
    {
        this.vm = vm;
        originalConnectionId = vm.Connection?.ConnectionData.Id ?? Guid.Empty;
        Results = results;
        
        TableController = new TableController(this);
        Title = $"Query {action.Index}";
        
        CopyInsertCommand = new DelegateCommand(() =>
        {
            if (TryGenerateInsert(Selection.All(), false, out var insert))
                vm.ClipboardService.SetText(insert);
        });
        
        CopyColumnNameCommand = new DelegateCommand(() =>
        {
            if (selectedCellIndex == -1 || selectedCellIndex >= Columns.Count)
                return;
            var name = Columns[selectedCellIndex].Header;
            vm.ClipboardService.SetText(name);
        });

        RefreshCommand = new AsyncAutoCommand(async () =>
        {
            ErrorIfConnectionChanged();
            await vm.ExecuteAndOverrideResultsAsync(action.Query, this);
        }).WrapMessageBox<Exception>(vm.UserQuestions);
        
        SelectAllCommand = new DelegateCommand(() =>
        {
            Selection.Replace(0, Count - 1);
        });
        
        CopySelectedCommand = new DelegateCommand(() =>
        {
            CsvWriter csv = new CsvWriter();
            var selectionItr = Selection.ContainsIterator;
            for (int i = 0; i < Count; ++i)
            {
                if (selectionItr.Contains(i))
                {
                    for (int j = 1; j < Columns.Count; ++j) // skip # column
                    {
                        var fullValue = GetFullValue(i, j);

                        csv.Write(fullValue);
                    }
                    csv.WriteLine();
                }
            }
            vm.ClipboardService.SetText(csv.ToString());
        });
        
        CreateViewCommand = new AsyncAutoCommand(async () =>
        {
            ErrorIfConnectionChanged();
            
            var newViewName = await vm.UserQuestions.AskForNewViewNameAsync();
            if (string.IsNullOrWhiteSpace(newViewName))
                return;
            
            try
            { 
                if (FromClause != null && !string.IsNullOrEmpty(FromClause.Value.Schema))
                    newViewName = $"`{FromClause.Value.Schema}`.`{newViewName}`";
                else
                    newViewName = $"`{newViewName}`";
                await vm.ExecuteAsync(new[] { $"CREATE VIEW {newViewName} AS {action.OriginalQuery}" }, false, true, false);
            }
            catch (Exception e)
            {
                await vm.UserQuestions.SaveErrorAsync(e);
            }
        }).WrapMessageBox<Exception>(vm.UserQuestions);
        
        On(() => SelectedRowIndex, _ => UpdateCellEditor());
        On(() => SelectedCellIndex, _ => UpdateCellEditor());
    }

    protected virtual bool IsColumnNullable(int columnIndex) => false;

    protected virtual string? ColumnType(int columnIndex) => null;
    
    protected virtual bool CanEditRow(int rowIndex) => false;
    
    private void UpdateCellEditor()
    {
        if (Selection.MoreThanOne)
        {
            CellEditor = null;
            return;
        }
        
        if (selectedCellIndex <= 0 || selectedCellIndex - 1 >= Results.Columns.Length)
        {
            CellEditor = null;
            return;
        }
        
        if (selectedRowIndex < 0 || selectedRowIndex >= Results.AffectedRows + additionalRowsCount)
        {
            CellEditor = null;
            return;
        }

        var isNullable = IsColumnNullable(selectedCellIndex);
        var type = ColumnType(selectedCellIndex);
        var column = Results.Columns[selectedCellIndex - 1];
        var overrideColumn = ColumnsOverride[selectedCellIndex - 1];
        BaseCellEditorViewModel? cellEditor = null;

        var isReadOnly = !CanEditRow(selectedRowIndex);
        
        if (column is ByteColumnData b)
            cellEditor = new UnsignedIntegerCellEditorViewModel(type, (ByteSparseColumnData)overrideColumn, b, selectedRowIndex, isNullable, isReadOnly);
        else if (column is UInt16ColumnData u)
            cellEditor = new UnsignedIntegerCellEditorViewModel(type, (UInt16SparseColumnData)overrideColumn, u, selectedRowIndex, isNullable, isReadOnly);
        else if (column is UInt32ColumnData u32)
            cellEditor = new UnsignedIntegerCellEditorViewModel(type, (UInt32SparseColumnData)overrideColumn, u32, selectedRowIndex, isNullable, isReadOnly);
        else if (column is UInt64ColumnData u64)
            cellEditor = new UnsignedIntegerCellEditorViewModel(type, (UInt64SparseColumnData)overrideColumn, u64, selectedRowIndex, isNullable, isReadOnly);
        else if (column is SByteColumnData sb)
            cellEditor = new SignedIntegerCellEditorViewModel(type, (SByteSparseColumnData)overrideColumn, sb, selectedRowIndex, isNullable, isReadOnly);
        else if (column is Int16ColumnData i16)
            cellEditor = new SignedIntegerCellEditorViewModel(type, (Int16SparseColumnData)overrideColumn, i16, selectedRowIndex, isNullable, isReadOnly);
        else if (column is Int32ColumnData i32)
            cellEditor = new SignedIntegerCellEditorViewModel(type, (Int32SparseColumnData)overrideColumn, i32, selectedRowIndex, isNullable, isReadOnly);
        else if (column is Int64ColumnData i64)
            cellEditor = new SignedIntegerCellEditorViewModel(type, (Int64SparseColumnData)overrideColumn, i64, selectedRowIndex, isNullable, isReadOnly);
        else if (column is DecimalColumnData dec)
            cellEditor = new DecimalCellEditorViewModel(type, (DecimalSparseColumnData)overrideColumn, dec, selectedRowIndex, isNullable, isReadOnly);
        else if (column is FloatColumnData f)
            cellEditor = new FloatCellEditorViewModel(type, (FloatSparseColumnData)overrideColumn, f, selectedRowIndex, isNullable, isReadOnly);
        else if (column is DoubleColumnData d)
            cellEditor = new DoubleCellEditorViewModel(type, (DoubleSparseColumnData)overrideColumn, d, selectedRowIndex, isNullable, isReadOnly);
        else if (column is StringColumnData s)
            cellEditor = new StringCellEditorViewModel(type, (StringSparseColumnData)overrideColumn, s, selectedRowIndex, isNullable, isReadOnly);
        else if (column is MySqlDateTimeColumnData dt)
            cellEditor = new MySqlDateTimeCellEditorViewModel(type, (MySqlDateTimeSparseColumnData)overrideColumn, dt, selectedRowIndex, isNullable, isReadOnly);
        else if (column is TimeSpanColumnData ts)
             cellEditor = new TimeSpanCellEditorViewModel(type, (TimeSpanSparseColumnData)overrideColumn, ts, selectedRowIndex, isNullable, isReadOnly);
        else if (column is BinaryColumnData bin)
            cellEditor = new BinaryCellEditorViewModel(vm.WindowManager, vm.UserQuestions, type, (BinarySparseColumnData)overrideColumn, bin, selectedRowIndex, isNullable, isReadOnly);
        else
            cellEditor = null;

        CellEditor = cellEditor;
    }

    protected void ErrorIfConnectionChanged()
    {
        if (vm.Connection?.ConnectionData.Id != originalConnectionId)
            throw new Exception("Connection changed, please re run the query as this tab is out of sync now.");
    }

    protected string ColumnValueToMySqlRepresentation(int columnIndex, string? value)
    {
        if (value == null)
            return "NULL";

        if (columnIndex - 1 >= Results.Columns.Length)
            return "NULL";
        
        var columnCategory = Results.Columns[columnIndex - 1]?.Category ?? ColumnTypeCategory.Unknown;

        if (columnCategory == ColumnTypeCategory.Unknown)
        {
            var columnName = Results.ColumnNames[columnIndex - 1];
            throw new Exception($"The column {columnName} has type {Results.Columns[columnIndex - 1]?.GetType()} which editor can't edit right now. Please report this.");
        }
            
        if (columnCategory is ColumnTypeCategory.String)
            return $"'{MySqlHelper.EscapeString(value)}'";
        else if (columnCategory is ColumnTypeCategory.DateTime)
        {
            if (value.Contains('(') && value.Contains(')')) // contains a function call
                return value;
            return $"'{value}'";
        }
        else if (columnCategory is ColumnTypeCategory.Binary)
        {
            return $"X'{value}'";
        }
            
        return value;
    }

    protected bool TryGenerateInsert(IEnumerable<int> rows, bool skipDeleted, out string insert)
    {
        List<string> inserts = new();
        foreach (var rowIndex in rows)
        {
            if (skipDeleted && IsRowMarkedAsDeleted(rowIndex))
                continue;
            
            List<string> columns = new();
            for (int columnIndex = 1; columnIndex < Columns.Count; ++columnIndex)
                columns.Add(ColumnValueToMySqlRepresentation(columnIndex, GetFullValue(rowIndex, columnIndex)));
            inserts.Add("(" + string.Join(", ", columns) + ")");
        }

        if (inserts.Count > 0)
        {
            var columns = string.Join(", ", Results.ColumnNames.Select(x => $"`{x}`"));
            var into = FromClause?.ToString() ?? "?";
            insert = $"INSERT INTO {into} ({columns}) VALUES\n" + string.Join(",\n", inserts);
            return true;
        }

        insert = "";
        return false;
    }
    
    protected virtual SimpleFrom? FromClause => null;

    protected void UpdateView()
    {
        // this trick will cause the view to be redrawn
        RequestRender = !requestRender;
    }

    public virtual void UpdateSelectedCells(string value)
    {
        vm.UserQuestions.InformCantEditNonSelectAsync().ListenErrors();
    }

    protected void MarkRowAsDeleted(int rowIndex)
    {
        deletedRows.Add(rowIndex);
        RaisePropertyChanged(nameof(IsModified));
        RaisePropertyChanged(nameof(TitleWithModifiedStatus));
        UpdateView();
    }

    public void UpdateResults(in SelectResult results, IReadOnlyList<ColumnInfo>? tableColumnsInfo)
    {
        ResetChanges();
        Results = results;
        ResultsUpdated(tableColumnsInfo);
        UpdateCellEditor();
        UpdateView();
        RaisePropertyChanged(nameof(Count));
        RaisePropertyChanged(nameof(Results));
    }
    
    protected virtual void ResultsUpdated(IReadOnlyList<ColumnInfo>? tableColumnsInfo) { }

    /// <summary>
    /// Returns full, not truncated, string representation of the value, not quoted, not escaped
    /// </summary>
    public string? GetFullValue(int rowIndex, int cellIndex)
    {
        if (cellIndex == 0) // # column
            return null;

        if (ColumnsOverride[cellIndex - 1].HasRow(rowIndex))
            return ColumnsOverride[cellIndex - 1].GetFullToString(rowIndex);
        
        if (rowIndex >= Results.AffectedRows)
            return null;
        
        if (cellIndex - 1 >= Results.Columns.Length)
            return null;
        
        return Results.Columns[cellIndex - 1]!.GetFullToString(rowIndex);
    }
    
    /// <summary>
    /// Returns short, maybe truncated, string representation of the value, not quoted, not escaped. I.e. binary data will be truncated
    /// </summary>
    /// <param name="rowIndex"></param>
    /// <param name="cellIndex"></param>
    /// <returns></returns>
    public string? GetShortValue(int rowIndex, int cellIndex)
    {
        if (cellIndex == 0) // # column
            return null;

        if (ColumnsOverride[cellIndex - 1].HasRow(rowIndex))
            return ColumnsOverride[cellIndex - 1].GetToString(rowIndex);

        if (rowIndex >= Results.AffectedRows)
            return null;

        if (cellIndex - 1 >= Results.Columns.Length)
            return null;
        
        return Results.Columns[cellIndex - 1]!.GetToString(rowIndex);
    }
    
    
    public bool IsRowMarkedAsDeleted(int rowIndex)
    {
        return deletedRows.Contains(rowIndex);
    }
    
    protected void OverrideValue(int rowIndex, int cellIndex, string? value)
    {
        if (cellIndex == 0) // # column
            return;
        
        if (!ColumnsOverride[cellIndex - 1]!.TryOverride(rowIndex, value, out var message))
        {
            vm.UserQuestions.InformEditErrorAsync(message!).ListenErrors();
        }
        else
        {
            alteredRows.Add(rowIndex);
            overridenCells.Add((rowIndex, cellIndex));
            if (rowIndex == selectedRowIndex &&
                cellIndex == selectedCellIndex)
                UpdateCellEditor();
        }
        RaisePropertyChanged(nameof(IsModified));
        RaisePropertyChanged(nameof(TitleWithModifiedStatus));
    }
    
    protected void ResetChanges()
    {
        deletedRows.Clear();
        overridenCells.Clear();
        alteredRows.Clear();
        AdditionalRowsCount = 0;
        ColumnsOverride.Each(x => x.Clear());
        UpdateView();
        RaisePropertyChanged(nameof(IsModified));
        RaisePropertyChanged(nameof(TitleWithModifiedStatus));
    }

    //protected SortedDictionary<int, Dictionary<int, string?>> overrides = new();

    protected SortedSet<int> alteredRows = new();
    
    protected HashSet<(int row, int cell)> overridenCells = new();
    
    protected SortedSet<int> deletedRows = new();

    public virtual async Task<bool> SaveAsync() => true;

    public bool TryGetRowOverride(int rowIndex, int cellIndex, out string? overrideString)
    {
        if (overridenCells.Contains((rowIndex, cellIndex)))
        {
            overrideString = ColumnsOverride[cellIndex - 1].GetToString(rowIndex);
            return true;
        }

        overrideString = null;
        return false;
    }
}

internal class TableController : BaseVirtualizedTableController
{
    private readonly SelectResultsViewModel vm;
    private static Bitmap? keyBitmap;

    public TableController(SelectResultsViewModel vm)
    {
        this.vm = vm;
    }

    static TableController()
    {
        if (GlobalApplication.Backend == GlobalApplication.AppBackend.UnitTests)
            return;
        keyBitmap = WdeImage.LoadBitmapNowOrAsync(new ImageUri("Icons/icon_key_mono.png"), bitmap =>
        {
            keyBitmap = bitmap;
        });
    }
    
    public override bool PointerDown(int rowIndex, int cellIndex, Rect cellRect, Point pressPoint, bool leftPressed, bool rightPressed, int clickCount)
    {
        return false;
    }

    public override bool PointerUp(int rowIndex, int cellIndex, Rect cellRect, Point pressPoint, bool leftPressed, bool rightPressed)
    {
        return false;
    }

    public override bool SpawnEditorFor(int rowIndex, int cellIndex, Rect cellRect, string? typedText, VirtualizedVeryFastTableView view)
    {
        if (cellIndex == 0) // # column
            return true; // meaning no editing
        return false;
    }

    public override string? GetCellText(int rowIndex, int cellIndex)
    {
        if (cellIndex == 0)
            return (rowIndex + 1).ToString(); // row index

        return vm.GetShortValue(rowIndex, cellIndex);
    }

    public override void DrawRow(int rowIndex, Rect rowRect, DrawingContext drawingContext, VirtualizedVeryFastTableView view)
    {
        if (vm.IsRowMarkedAsDeleted(rowIndex))
        {
            drawingContext.DrawRectangle(new SolidColorBrush(Colors.Red, 0.2f), null, rowRect);
        }
    }

    public override bool Draw(int rowIndex, int cellIndex, DrawingContext drawingContext, VirtualizedVeryFastTableView view, ref Rect rect)
    {
        if (cellIndex == 0)
        {
            rect = rect.Deflate(new Thickness(0, 0, 5, 0));
            DrawText(drawingContext, rect, (rowIndex + 1).ToString(), alignment: TextAlignment.Right);
            return true;
        }

        string? overrideString = null;
        var hasOverride = vm.TryGetRowOverride(rowIndex, cellIndex, out overrideString);
        if (hasOverride)
        {
            drawingContext.DrawRectangle(null, new Pen(Brushes.Orange), rect.Deflate(1));
        }

        if ((hasOverride && overrideString == null) || (!hasOverride && rowIndex < vm.Results.AffectedRows && cellIndex - 1 < vm.Results.Columns.Length && vm.Results.Columns[cellIndex - 1]!.IsNull(rowIndex)))
        {
            DrawText(drawingContext, rect, "(null)");
            return true;
        }
        return false;
    }

    public override void DrawHeader(int cellIndex, DrawingContext context, VirtualizedVeryFastTableView view, ref Rect rect)
    {
        if (vm is not SelectSingleTableViewModel selectViewModel)
            return;

        if (selectViewModel.IsColumnPrimaryKey(cellIndex))
        {
            var imageRect = new Rect(Math.Max(rect.Left, rect.Right - 16), rect.Center.Y - 8, Math.Min(16, rect.Width), 16);
            if (keyBitmap != null)
            {
                context.DrawImage(keyBitmap, imageRect);
            }
            else
            {
                DrawText(context, imageRect, "PK");
                rect = rect.Deflate(new Thickness(10, 0, 0, 0));   
            }
            rect = rect.Deflate(new Thickness(0, 0, imageRect.Width, 0));
        }
    }
}