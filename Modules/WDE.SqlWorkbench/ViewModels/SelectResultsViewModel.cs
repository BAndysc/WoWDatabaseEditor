using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvaloniaStyles.Controls.FastTableView;
using AvaloniaStyles.Controls.OptimizedVeryFastTableView;
using MySqlConnector;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.Avalonia.Components;
using WDE.Common.Services.MessageBox;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Services.Connection;
using WDE.SqlWorkbench.Utils;

namespace WDE.SqlWorkbench.ViewModels;

internal partial class SelectResultsViewModel : ObservableBase
{
    private readonly SqlWorkbenchViewModel vm;
    [Notify] private Vector scrollOffset;
    [Notify] private int selectedRowIndex;
    [Notify] private int selectedCellIndex;
    [Notify] [AlsoNotify(nameof(Count), nameof(IsModified))] private int additionalRowsCount;
    [Notify] private bool requestRender;
    
    public bool IsModified => overrides.Count > 0 || deletedRows.Count > 0 || additionalRowsCount > 0;

    public string TitleWithModifiedStatus => IsModified ? Title + " *" : Title;
    
    public IMultiIndexContainer Selection { get; } = new MultiIndexContainer();
    public string Title { get; }
    public IReadOnlyList<ITableColumnHeader> Columns { get; }
    
    public int Count => Results.AffectedRows + AdditionalRowsCount;
    
    public TableController TableController { get; }
    
    public SelectResult Results { get; protected set; }
    
    public virtual ImageUri Icon => new("Icons/icon_mini_view_big.png");
    
    public ICommand CopyColumnNameCommand { get; }
    
    public ICommand SelectAllCommand { get; }
    
    public ICommand CopySelectedCommand { get; }
    
    public SelectResultsViewModel(SqlWorkbenchViewModel vm, string title, in SelectResult results)
    {
        this.vm = vm;
        Results = results;
        TableController = new TableController(this);
        Title = title;
        Columns = results.ColumnNames.Select(x => new TableTableColumnHeader(x)).Prepend(new TableTableColumnHeader("#", 50)).ToArray();
        
        CopyColumnNameCommand = new DelegateCommand(() =>
        {
            if (selectedCellIndex == -1 || selectedCellIndex >= Columns.Count)
                return;
            var name = Columns[selectedCellIndex].Header;
            vm.ClipboardService.SetText(name);
        });
        
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
    }

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

    /// <summary>
    /// Returns full, not truncated, string representation of the value, not quoted, not escaped
    /// </summary>
    public string? GetFullValue(int rowIndex, int cellIndex)
    {
        if (TryGetRowOverride(rowIndex, cellIndex, out var overrideString))
            return overrideString;
        
        if (rowIndex >= Results.AffectedRows)
            return null;

        if (cellIndex == 0) // # column
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
        if (TryGetRowOverride(rowIndex, cellIndex, out var overrideString))
            return overrideString;
        
        if (rowIndex >= Results.AffectedRows)
            return null;

        if (cellIndex == 0) // # column
            return null;
        
        return Results.Columns[cellIndex - 1]!.GetToString(rowIndex);
    }
    
    public bool TryGetRowOverride(int rowIndex, int cellIndex, out string? value)
    {
        if (overrides.TryGetValue(rowIndex, out var rowOverride) &&
            rowOverride.TryGetValue(cellIndex, out var overrideString))
        {
            value = overrideString;
            return true;
        }

        value = null;
        return false;
    }
    
    public bool IsRowMarkedAsDeleted(int rowIndex)
    {
        return deletedRows.Contains(rowIndex);
    }
    
    protected void OverrideValue(int rowIndex, int cellIndex, string? value)
    {
        if (!overrides.TryGetValue(rowIndex, out var rowOverride))
            rowOverride = overrides[rowIndex] = new Dictionary<int, string?>();
        rowOverride[cellIndex] = value;
        RaisePropertyChanged(nameof(IsModified));
        RaisePropertyChanged(nameof(TitleWithModifiedStatus));
    }
    
    protected void ResetChanges()
    {
        deletedRows.Clear();
        overrides.Clear();
        AdditionalRowsCount = 0;
        UpdateView();
        RaisePropertyChanged(nameof(IsModified));
        RaisePropertyChanged(nameof(TitleWithModifiedStatus));
    }

    protected SortedDictionary<int, Dictionary<int, string?>> overrides = new();

    protected SortedSet<int> deletedRows = new();

    public virtual async Task<bool> SaveAsync() => true;
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
        keyBitmap = WdeImage.LoadBitmap(new ImageUri("Icons/icon_key_mono.png"));
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
            DrawText(drawingContext, rect, (rowIndex + 1).ToString());
            return true;
        }

        string? overrideString = null;
        var hasOverride = vm.TryGetRowOverride(rowIndex, cellIndex, out overrideString);
        if (hasOverride)
        {
            drawingContext.DrawRectangle(null, new Pen(Brushes.Orange), rect.Deflate(1));
        }

        if ((hasOverride && overrideString == null) || rowIndex < vm.Results.AffectedRows && vm.Results.Columns[cellIndex - 1]!.IsNull(rowIndex))
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