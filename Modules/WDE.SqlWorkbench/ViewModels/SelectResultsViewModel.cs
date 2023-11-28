using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvaloniaStyles.Controls.FastTableView;
using AvaloniaStyles.Controls.OptimizedVeryFastTableView;
using PropertyChanged.SourceGenerator;
using WDE.Common.Avalonia.Components;
using WDE.Common.Services.MessageBox;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.SqlWorkbench.Services.Connection;

namespace WDE.SqlWorkbench.ViewModels;

internal partial class SelectResultsViewModel : ObservableBase
{
    private readonly SqlWorkbenchViewModel vm;
    [Notify] private Vector scrollOffset;
    [Notify] private int selectedRowIndex;
    [Notify] private int selectedCellIndex;
    [Notify] private bool requestRender;

    public IMultiIndexContainer Selection { get; } = new MultiIndexContainer();
    public string Title { get; }
    public IReadOnlyList<ITableColumnHeader> Columns { get; }
    
    public int Count => Results.AffectedRows;
    
    public TableController TableController { get; }
    
    public SelectResult Results { get; protected set; }
    
    public virtual ImageUri Icon => new("Icons/icon_mini_view_big.png");
    
    public SelectResultsViewModel(SqlWorkbenchViewModel vm, string title, in SelectResult results)
    {
        this.vm = vm;
        Results = results;
        TableController = new TableController(this);
        Title = title;
        Columns = results.ColumnNames.Select(x => new TableTableColumnHeader(x)).Prepend(new TableTableColumnHeader("#", 50)).ToArray();
    }

    protected void UpdateView()
    {
        // this trick will cause the view to be redrawn
        RequestRender = !requestRender;
    }

    public virtual void UpdateSelectedCells(string value)
    {
        vm.MessageBoxService.SimpleDialog("Error", 
            "Can't edit this query", 
            "You can't edit cells in this query, because this is not a simple SELECT query.").ListenErrors();
    }

    public Dictionary<(int row, int cell), string?> overrides = new();

    public virtual int VirtualIndexToResultsIndex(int rowIndex) => rowIndex;
    
    public HashSet<int> deletedRows = new();
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

        if (vm.overrides.TryGetValue((rowIndex, cellIndex), out var overrideString))
            return overrideString;
        
        return vm.Results.Columns[cellIndex - 1]!.GetToString(rowIndex);
    }

    public override void DrawRow(int rowIndex, Rect rowRect, DrawingContext drawingContext, VirtualizedVeryFastTableView view)
    {
        if (vm.deletedRows.Contains(rowIndex))
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

        var hasOverride = vm.overrides.TryGetValue((rowIndex, cellIndex), out var overrideString);
        if (hasOverride)
        {
            drawingContext.DrawRectangle(null, new Pen(Brushes.Orange), rect.Deflate(1));
        }

        if ((hasOverride && overrideString == null) || vm.Results.Columns[cellIndex - 1]!.IsNull(rowIndex))
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