using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using WDE.Common.Avalonia.DnD;
using WDE.DbScriptsEditor.Editor.ViewModels;

namespace WDE.DbScriptsEditor.Avalonia.Editor.Views
{
    public partial class DbScriptEditorView : UserControl
    {
        private ItemsControl? timelineItems;
        private Canvas? dropOverlay;
        private Border? dropIndicator;

        // The gap (0..Rows.Count) the current drag would insert at, updated on every DragOver.
        private int pendingInsertIndex = -1;
        // At an ambiguous block boundary (right after the last member of an if block, or after an
        // empty if) the pointer's VERTICAL half decides: hovering the bottom half of the row
        // above the gap = drop INSIDE its block; hovering the top half of the row below the gap
        // (or past the end of the list) = drop OUTSIDE. The indicator indents to preview it.
        private bool pendingPreferInside;
        private const double BlockIndent = 18;

        public DbScriptEditorView()
        {
            InitializeComponent();
            AddHandler(DragDrop.DragOverEvent, OnDragOver);
            AddHandler(DragDrop.DropEvent, OnDrop);
            AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            timelineItems = this.FindControl<ItemsControl>("TimelineItems");
            dropOverlay = this.FindControl<Canvas>("DropOverlay");
            dropIndicator = this.FindControl<Border>("DropIndicator");
        }

        private static bool TryGetPayload(DragEventArgs e, out DbScriptRowViewModel payload)
        {
            if (e.DataTransfer.TryGet<DbScriptRowViewModel>(out var row) && row != null)
            {
                payload = row;
                return true;
            }
            payload = null!;
            return false;
        }

        private void OnDragOver(object? sender, DragEventArgs e)
        {
            if (!TryGetPayload(e, out _))
            {
                e.DragEffects = DragDropEffects.None;
                HideIndicator();
                e.Handled = true;
                return;
            }

            e.DragEffects = DragDropEffects.Move;
            UpdateInsertionIndicator(e);
            e.Handled = true;
        }

        // Finds the gap between timeline rows nearest to the pointer and positions the insertion
        // line there (SmartScript-style visual feedback of where the drop will land).
        private void UpdateInsertionIndicator(DragEventArgs e)
        {
            if (timelineItems == null || dropOverlay == null || dropIndicator == null)
                return;

            var pointerY = e.GetPosition(dropOverlay).Y;
            var count = timelineItems.ItemCount;
            var index = count;
            double indicatorY = 0;
            var found = false;
            double lastBottom = 0;
            var anyContainer = false;

            for (var i = 0; i < count; i++)
            {
                var container = timelineItems.ContainerFromIndex(i);
                if (container == null || !container.IsVisible)
                    continue;
                var topLeft = container.TranslatePoint(new Point(0, 0), dropOverlay);
                if (topLeft == null)
                    continue;
                anyContainer = true;
                var top = topLeft.Value.Y;
                var bottom = top + container.Bounds.Height;
                lastBottom = bottom;
                if (!found && pointerY < (top + bottom) / 2)
                {
                    index = i;
                    indicatorY = top;
                    found = true;
                }
            }

            if (!anyContainer)
            {
                HideIndicator();
                pendingInsertIndex = 0;
                return;
            }

            if (!found)
                indicatorY = lastBottom;

            pendingInsertIndex = index;

            // Both halves around a gap map to the same insertion index, so WHICH half the pointer
            // is in carries the inside/outside intent: above the gap line = still over the row
            // above (join its block), at/below the gap line = over the next row / past the end
            // (stay outside).
            pendingPreferInside = pointerY < indicatorY || (!found && pointerY <= lastBottom);
            var touchesBlock = false;
            if (index > 0 && timelineItems.ItemsSource is System.Collections.IList list && index <= list.Count)
            {
                var above = list[index - 1];
                touchesBlock = above is DbScriptIfViewModel ||
                               above is DbScriptRowViewModel { InConditionBlock: true };
            }
            var indent = touchesBlock && pendingPreferInside ? BlockIndent : 0;

            dropIndicator.Width = dropOverlay.Bounds.Width - 14 - indent;
            Canvas.SetLeft(dropIndicator, 7 + indent);
            Canvas.SetTop(dropIndicator, indicatorY - 1.5);
            dropIndicator.IsVisible = true;
        }

        private void HideIndicator()
        {
            if (dropIndicator != null)
                dropIndicator.IsVisible = false;
        }

        private void OnDragLeave(object? sender, DragEventArgs e)
        {
            HideIndicator();
        }

        private void OnDrop(object? sender, DragEventArgs e)
        {
            HideIndicator();
            if (!TryGetPayload(e, out var payload) ||
                DataContext is not DbScriptEditorViewModel vm ||
                pendingInsertIndex < 0)
                return;

            vm.DropAt(payload, pendingInsertIndex, pendingPreferInside);
            pendingInsertIndex = -1;
            e.Handled = true;
        }
    }
}
