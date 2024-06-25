using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaStyles.Controls;
using WDE.Common.Utils;
using WDE.Common.Utils.DragDrop;
using DragDropEffects = Avalonia.Input.DragDropEffects;
#pragma warning disable CS0618 // Type or member is obsolete

namespace WDE.Common.Avalonia.DnD
{
    public class DragAndDrop
    {
        public static readonly AttachedProperty<bool> IsDropTargetProperty = AvaloniaProperty.RegisterAttached<AvaloniaObject, bool>("IsDropTarget", typeof(DragAndDrop));
        public static readonly AttachedProperty<bool> IsDragSourceProperty = AvaloniaProperty.RegisterAttached<AvaloniaObject, bool>("IsDragSource", typeof(DragAndDrop));
        public static readonly AttachedProperty<IDropTarget?> DropHandlerProperty = AvaloniaProperty.RegisterAttached<AvaloniaObject, IDropTarget?>("DropHandler", typeof(DragAndDrop));
        public static readonly AttachedProperty<IDragSource?> DragHandlerProperty = AvaloniaProperty.RegisterAttached<AvaloniaObject, IDragSource?>("DragHandler", typeof(DragAndDrop));

        public static bool GetIsDropTarget(AvaloniaObject obj)
        {
            return (bool?) obj.GetValue(IsDropTargetProperty) ?? false;
        }

        public static void SetIsDropTarget(AvaloniaObject obj, bool value)
        {
            obj.SetValue(IsDropTargetProperty, value);
        }

        public static bool GetIsDragSource(AvaloniaObject obj)
        {
            return (bool?) obj.GetValue(IsDragSourceProperty) ?? false;
        }

        public static void SetIsDragSource(AvaloniaObject obj, bool value)
        {
            obj.SetValue(IsDragSourceProperty, value);
        }

        public static IDropTarget? GetDropHandler(AvaloniaObject obj)
        {
            return (IDropTarget?) obj.GetValue(DropHandlerProperty);
        }

        public static void SetDropHandler(AvaloniaObject obj, IDropTarget? value)
        {
            obj.SetValue(DropHandlerProperty, value);
        }

        public static IDragSource? GetDragHandler(AvaloniaObject obj)
        {
            return (IDragSource?) obj.GetValue(DragHandlerProperty);
        }

        public static void SetDragHandler(AvaloniaObject obj, IDragSource? value)
        {
            obj.SetValue(DragHandlerProperty, value);
        }
        
        private static Point m_cursorStartPos;
        private static AdornerHelper adorner;

        private static IDropInfo? dropInfo;
        
        private static readonly KeyModifiers platformCopyKeyModifier;

        static DragAndDrop()
        {
            platformCopyKeyModifier = KeyGestures.CommandModifier;
            adorner = new AdornerHelper();
            IsDropTargetProperty.Changed.Subscribe(args =>
            {
                if (args.Sender is ListBox listBox)
                {
                    args.Sender.SetValue(DragDrop.AllowDropProperty, true);
                    listBox.AddHandler(DragDrop.DropEvent, OnListDrop);
                    listBox.AddHandler(DragDrop.DragOverEvent, OnListDragOver);
                }
                else if (args.Sender is TreeView treeView)
                {
                    args.Sender.SetValue(DragDrop.AllowDropProperty, true);
                    treeView.AddHandler(DragDrop.DropEvent, OnTreeViewDrop);
                    treeView.AddHandler(DragDrop.DragOverEvent, OnTreeViewDragOver);
                }
                else if (args.Sender is GridView gridView)
                {
                    args.Sender.SetValue(DragDrop.AllowDropProperty, true);
                    gridView.AddHandler(DragDrop.DropEvent, OnGridViewDrop);
                    gridView.AddHandler(DragDrop.DragOverEvent, OnGridViewDragOver);
                }
            });
            IsDragSourceProperty.Changed.Subscribe(args =>
            {
                if (args.Sender is ListBox listBox)
                {
                    listBox.AddHandler(InputElement.PointerMovedEvent, OnListPreviewMouseMove, RoutingStrategies.Tunnel);
                    listBox.AddHandler(InputElement.PointerPressedEvent, OnListPreviewMouseLeftButtonDown, RoutingStrategies.Tunnel);
                }
                else if (args.Sender is TreeView treeView)
                {
                    treeView.AddHandler(InputElement.PointerMovedEvent, OnTreeViewPreviewMouseMove, RoutingStrategies.Tunnel);
                    treeView.AddHandler(InputElement.PointerPressedEvent, OnListPreviewMouseLeftButtonDown, RoutingStrategies.Tunnel);
                }
                else if (args.Sender is GridView gridView)
                {
                    gridView.AddHandler(InputElement.PointerMovedEvent, OnGridViewPreviewMouseMove, RoutingStrategies.Tunnel);
                    gridView.AddHandler(InputElement.PointerPressedEvent, OnListPreviewMouseLeftButtonDown, RoutingStrategies.Tunnel);
                }
            });
        }
        
        private static bool dragging = false;

        private static void OnListPreviewMouseLeftButtonDown(object? sender, PointerEventArgs e)
        {
            dragging = false;
            m_cursorStartPos = e.GetPosition(null);
        }

        private static void OnTreeViewPreviewMouseMove(object? sender, PointerEventArgs e)
        {
            if (sender is not TreeView treeView)
                return;

            Point currentCursorPos = e.GetPosition(null);
            Vector cursorVector = m_cursorStartPos - currentCursorPos;

            if (!e.GetCurrentPoint(treeView).Properties.IsLeftButtonPressed ||
                (!(Math.Abs(cursorVector.X) > 10) && !(Math.Abs(cursorVector.Y) > 10)) || dragging)
                return;
            
            TreeViewItem? targetItem = FindVisualParent<TreeViewItem>((Visual?)e.Source);
            if (targetItem == null || targetItem.DataContext == null) 
                return;
            
            var dragHandler = GetDragHandler(treeView);
            if (treeView.SelectedItem == null || dragHandler != null && !dragHandler.CanDrag(treeView.SelectedItem))
                return;
            
            dragging = true;
            var data = new DataObject();
            data.Set("", new DragInfo()
            {
                draggedElement = new List<object?>(){treeView.SelectedItem},
                draggedIndex = new List<int>(){0}
            });

            DoDrag(e, treeView, data).ListenErrors();
        }
        
        private static void OnGridViewPreviewMouseMove(object? sender, PointerEventArgs e)
        {
            GridView? listBox = sender as GridView;

            if (listBox == null)
                return;
            
            Point currentCursorPos = e.GetPosition(null);
            Vector cursorVector = m_cursorStartPos - currentCursorPos;

            if (!e.GetCurrentPoint(listBox).Properties.IsLeftButtonPressed ||
                (!(Math.Abs(cursorVector.X) > 10) && !(Math.Abs(cursorVector.Y) > 10)) || dragging)
                return;
            
            ListBoxItem? targetItem = FindVisualParent<ListBoxItem>((Visual?)e.Source);
            if (targetItem == null || targetItem.DataContext == null) 
                return;
            
            dragging = true;
            var data = new DataObject();
            data.Set("", new DragInfo()
            {
                draggedElement = listBox.ListBoxImpl!.Selection.SelectedItems,
                draggedIndex = listBox.ListBoxImpl!.Selection.SelectedIndexes
            });
                    
            DoDrag(e, listBox.ListBoxImpl!, data).ListenErrors();
        }
            
        private static void OnListPreviewMouseMove(object? sender, PointerEventArgs e)
        {
            ListBox? listBox = sender as ListBox;

            if (listBox == null)
                return;
            
            Point currentCursorPos = e.GetPosition(null);
            Vector cursorVector = m_cursorStartPos - currentCursorPos;

            if (!e.GetCurrentPoint(listBox).Properties.IsLeftButtonPressed ||
                (!(Math.Abs(cursorVector.X) > 10) && !(Math.Abs(cursorVector.Y) > 10)) || dragging)
                return;
            
            ListBoxItem? targetItem = FindVisualParent<ListBoxItem>((Visual?)e.Source);
            if (targetItem == null || targetItem.DataContext == null) 
                return;
            
            dragging = true;
            var data = new DataObject();
            data.Set("", new DragInfo()
            {
                draggedElement = listBox.Selection.SelectedItems,
                draggedIndex = listBox.Selection.SelectedIndexes
            });
                    
            DoDrag(e, listBox, data).ListenErrors();
        }

        private static async Task DoDrag(PointerEventArgs e, Visual listBox, DataObject data)
        {
            adorner.AddAdorner(listBox);
            await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
            adorner.RemoveAdorner(listBox);
        }

        public struct DragInfo 
        {
            public IReadOnlyList<int> draggedIndex;
            public IReadOnlyList<object?> draggedElement;
        }

        public class DropInfo : IDropInfo
        {
            public DropInfo(object data)
            {
                Data = data;
            }

            public object Data { get; set; }
            public object? TargetItem { get; set; }
            public int InsertIndex { get; set; }
            public DropTargetAdorners DropTargetAdorner { get; set; }
            public Common.Utils.DragDrop.DragDropEffects Effects { get; set; }
            public RelativeInsertPosition InsertPosition { get; set; }
            public bool IsCopy { get; set; }
        }

        private static void OnListDragOver(object? sender, DragEventArgs e)
        {
            if (e.Data.Contains(DataFormats.Files))
                return;

            var listBox = sender as ListBox;
            var dropElement = FindVisualParent<ListBoxItem>(e.Source as Visual);
            var dragInfo = e.Data.Get("") as DragInfo?;
            if (dragInfo == null || dragInfo.Value.draggedElement.Count == 0)
                return;
            
            if (listBox == null)
                return;

            var dropHandler = GetDropHandler(listBox);
            if (dropHandler == null)
                return;

            var indexOfDrop = dropElement != null ? listBox.ItemContainerGenerator.IndexFromContainer(dropElement) : -1;
            RelativeInsertPosition insertPosition = RelativeInsertPosition.None;
            
            if (dropElement != null)
            {
                var rel = e.GetPosition(dropElement).Y / dropElement.Bounds.Height;
                if (rel < 0.5f)
                    insertPosition = RelativeInsertPosition.BeforeTargetItem;
                else
                    insertPosition = RelativeInsertPosition.AfterTargetItem;

                if (rel >= 0.25f && rel <= 0.75f)
                    insertPosition |= RelativeInsertPosition.TargetItemCenter;
            }
            else
                indexOfDrop = listBox.ItemCount;

            var dropInfo = new DropInfo(dragInfo.Value.draggedElement[0]!)
            {
                InsertIndex = indexOfDrop,
                InsertPosition = insertPosition,
                TargetItem = listBox.Items,
                IsCopy = IsCopyKey(e)
            };
            dropHandler.DragOver(dropInfo);
            e.Handled = true;

            {
                double mousePosY = e.GetPosition(listBox).Y;
                if (mousePosY < 10)
                {
                    listBox.Scroll!.Offset = listBox.Scroll.Offset + new Vector(0, -1);
                }
                else if (mousePosY > listBox.Bounds.Height - 10)
                {
                    listBox.Scroll!.Offset = listBox.Scroll.Offset + new Vector(0, +1);
                }
            }
            
            adorner.Adorner?.Update(listBox, dropInfo);
        }

        private static void OnListDrop(object? sender, DragEventArgs e)
        {
            if (e.Data.Contains(DataFormats.Files))
                return;

            var listBox = sender as ListBox;
            var dropElement = FindVisualParent<ListBoxItem>(e.Source as Visual);
            var dragInfo = e.Data.Get("") as DragInfo?;
            if (dragInfo == null || dragInfo.Value.draggedElement.Count == 0)
                return;
            
            if (listBox == null)
                return;
            
            var dropHandler = GetDropHandler(listBox);
            if (dropHandler == null)
                return;
            
            adorner.RemoveAdorner(listBox);
            
            var indexOfDrop = dropElement != null ? listBox.ItemContainerGenerator.IndexFromContainer(dropElement) : -1;
            if (dropElement != null)
            {
                var pos = e.GetPosition(dropElement);
                if (pos.Y > dropElement.Bounds.Height / 2)
                    indexOfDrop++;
            }
            else
                indexOfDrop = listBox.ItemCount;
            
            WrapTryCatch(() =>
            {
                dropHandler.Drop(new DropInfo(dragInfo.Value.draggedElement[0]!)
                {
                    InsertIndex = indexOfDrop,
                    TargetItem = listBox.Items,
                    IsCopy = IsCopyKey(e)
                });
                e.Handled = true;
            });
        }
        
        
        
        
        // Grid View
        private static void OnGridViewDragOver(object? sender, DragEventArgs e)
        {
            if (e.Data.Contains(DataFormats.Files))
                return;

            var listBox = sender as GridView;
            var dropElement = FindVisualParent<ListBoxItem>(e.Source as Visual);
            var dragInfo = e.Data.Get("") as DragInfo?;
            if (dragInfo == null || dragInfo.Value.draggedElement.Count == 0)
                return;
            
            if (listBox == null)
                return;

            var dropHandler = GetDropHandler(listBox);
            if (dropHandler == null)
                return;
            
            var indexOfDrop = dropElement != null ? listBox.ListBoxImpl!.ItemContainerGenerator.IndexFromContainer(dropElement) : -1;
            RelativeInsertPosition insertPosition = RelativeInsertPosition.None;
            
            if (dropElement != null)
            {
                var rel = e.GetPosition(dropElement).Y / dropElement.Bounds.Height;
                if (rel < 0.5f)
                    insertPosition = RelativeInsertPosition.BeforeTargetItem;
                else
                    insertPosition = RelativeInsertPosition.AfterTargetItem;

                if (rel >= 0.25f && rel <= 0.75f)
                    insertPosition |= RelativeInsertPosition.TargetItemCenter;
            }
            else
                indexOfDrop = listBox.ListBoxImpl!.ItemCount;

            var dropInfo = new DropInfo(listBox.ListBoxImpl?.SelectionMode == SelectionMode.Multiple ? dragInfo.Value.draggedElement : dragInfo.Value.draggedElement[0]!)
            {
                InsertIndex = indexOfDrop,
                InsertPosition = insertPosition,
                TargetItem = listBox.Items,
                IsCopy = IsCopyKey(e)
            };
            dropHandler.DragOver(dropInfo);
            e.Handled = true;

            {
                double mousePosY = e.GetPosition(listBox).Y;
                if (mousePosY < 10)
                {
                    listBox.ListBoxImpl!.Scroll!.Offset = listBox.ListBoxImpl!.Scroll.Offset + new Vector(0, -1);
                }
                else if (mousePosY > listBox.Bounds.Height - 10)
                {
                    listBox.ListBoxImpl!.Scroll!.Offset = listBox.ListBoxImpl!.Scroll.Offset + new Vector(0, +1);
                }
            }
            
            adorner.Adorner?.Update(listBox.ListBoxImpl!, dropInfo);
        }
        
        private static void OnGridViewDrop(object? sender, DragEventArgs e)
        {
            if (e.Data.Contains(DataFormats.Files))
                return;

            var listBox = sender as GridView;
            var dropElement = FindVisualParent<ListBoxItem>(e.Source as Visual);
            var dragInfo = e.Data.Get("") as DragInfo?;
            if (dragInfo == null || dragInfo.Value.draggedElement.Count == 0)
                return;
            
            if (listBox == null)
                return;
            
            var dropHandler = GetDropHandler(listBox);
            if (dropHandler == null)
                return;
            
            adorner.RemoveAdorner(listBox.ListBoxImpl!);
            
            var indexOfDrop = dropElement != null ? listBox.ListBoxImpl!.ItemContainerGenerator.IndexFromContainer(dropElement) : -1;
            if (dropElement != null)
            {
                var pos = e.GetPosition(dropElement);
                if (pos.Y > dropElement.Bounds.Height / 2)
                    indexOfDrop++;
            }
            else
                indexOfDrop = listBox.ListBoxImpl!.ItemCount;

            WrapTryCatch(() =>
            {
                dropHandler.Drop(new DropInfo(listBox.ListBoxImpl?.SelectionMode == SelectionMode.Multiple
                    ? dragInfo.Value.draggedElement
                    : dragInfo.Value.draggedElement[0]!)
                {
                    InsertIndex = indexOfDrop,
                    TargetItem = listBox.Items,
                    IsCopy = IsCopyKey(e)
                });
                e.Handled = true;
            });
        }
        
        
        
        

        private static void OnTreeViewDragOver(object? sender, DragEventArgs e)
        {
            if (e.Data.Contains(DataFormats.Files))
                return;

            var treeView = sender as TreeView;
            var dropElement = FindVisualParent<TreeViewItem>(e.Source as Visual);
            var dragInfo = e.Data.Get("") as DragInfo?;
            if (dragInfo == null || dragInfo.Value.draggedElement.Count == 0)
                return;
            
            if (treeView == null)
                return;

            var dropHandler = GetDropHandler(treeView);
            if (dropHandler == null)
                return;

            var parent = dropElement == null ? treeView : FindVisualParent<TreeView, TreeViewItem>(dropElement);
            
            ItemContainerGenerator treeItemContainerGenerator;
            if (parent is TreeView tv)
                treeItemContainerGenerator = tv.ItemContainerGenerator;
            else if (parent is TreeViewItem ti)
                treeItemContainerGenerator = ti.ItemContainerGenerator;
            else
                return;
            
            adorner.AddAdorner(treeView); // parent
            var indexOfDrop = dropElement != null ? treeItemContainerGenerator.IndexFromContainer(dropElement) : -1;
            RelativeInsertPosition insertPosition = RelativeInsertPosition.None;
            
            if (dropElement != null)
            {
                var header = dropElement.GetVisualChildren().FirstOrDefault()?.GetVisualChildren().FirstOrDefault();
                var height = header?.Bounds.Height ?? dropElement.Bounds.Height;
                
                var rel = e.GetPosition(dropElement).Y / height;
                if (rel < 0.5f)
                    insertPosition = RelativeInsertPosition.BeforeTargetItem;
                else
                    insertPosition = RelativeInsertPosition.AfterTargetItem;

                if (rel >= 0.25f && rel <= 0.75f)
                    insertPosition |= RelativeInsertPosition.TargetItemCenter;
            }
            else
                indexOfDrop = treeView.ItemCount;

            if (insertPosition.HasFlagFast(RelativeInsertPosition.AfterTargetItem) &&
                (dropElement?.IsExpanded ?? false) &&
                dropElement.ItemCount > 0)
            {
                indexOfDrop = 0;
                insertPosition = RelativeInsertPosition.BeforeTargetItem;
                dropElement = (TreeViewItem?) dropElement.ContainerFromIndex(0);
            }
            
            dropInfo = new DropInfo(dragInfo.Value.draggedElement[0]!)
            {
                InsertIndex = indexOfDrop,
                InsertPosition = insertPosition,
                TargetItem = ((Control?)dropElement)?.DataContext,
                IsCopy = IsCopyKey(e)
            };
            dropHandler.DragOver(dropInfo);
            e.Handled = true;

            /*{
                double mousePosY = e.GetPosition(listBox).Y;
                if (mousePosY < 10)
                {
                    listBox.Scroll.Offset = listBox.Scroll.Offset + new Vector(0, -1);
                }
                else if (mousePosY > listBox.Bounds.Height - 10)
                {
                    listBox.Scroll.Offset = listBox.Scroll.Offset + new Vector(0, +1);
                }
            }*/
            
            if (dropInfo.DropTargetAdorner == DropTargetAdorners.Insert)
                dropInfo.InsertPosition =
                    (RelativeInsertPosition) ((int) dropInfo.InsertPosition &
                                              ~(int) RelativeInsertPosition.TargetItemCenter);
            else if (dropInfo.DropTargetAdorner == DropTargetAdorners.Highlight)
                dropInfo.InsertPosition &= ~(RelativeInsertPosition.AfterTargetItem | RelativeInsertPosition.BeforeTargetItem);
            adorner.Adorner?.Update(treeView, treeItemContainerGenerator, dropInfo);
        }

        private static void OnTreeViewDrop(object? sender, DragEventArgs e)
        {
            if (e.Data.Contains(DataFormats.Files))
                return;

            var treeView = sender as TreeView;
            var dropElement = FindVisualParent<TreeViewItem>(e.Source as Visual);
            var dragInfo = e.Data.Get("") as DragInfo?;
            if (dragInfo == null || dragInfo.Value.draggedElement.Count == 0)
                return;
            
            if (treeView == null)
                return;
            
            var dropHandler = GetDropHandler(treeView);
            if (dropHandler == null)
                return;
            
            adorner.RemoveAdorner(treeView);
            
            var scrollViewer = treeView.FindDescendantOfType<ScrollViewer>();
            var previousOffset = scrollViewer?.Offset;

            if (dropInfo != null && dropInfo.Effects != Common.Utils.DragDrop.DragDropEffects.None)
                WrapTryCatch(() => dropHandler.Drop(dropInfo));
            dropInfo = null;
            
            Dispatcher.UIThread.Post(() =>
            {
                if (previousOffset.HasValue)
                    scrollViewer!.Offset = previousOffset.Value;
            }, DispatcherPriority.Render);
            e.Handled = true;
        }

        private static void WrapTryCatch(Action x)
        {
            try
            {
                x();
            }
            catch (Exception e)
            {
                LOG.LogError(e);
            }
        }

        private static T? FindVisualParent<T>(Visual? child) where T : Visual
        {
            while (true)
            {
                if (child == null) 
                    return default;

                Visual? parentObject = child.GetVisualParent();
                if (parentObject == null) 
                    return default;

                if (parentObject is T parent) 
                    return parent;

                child = parentObject;
            }
        }

        private static Visual? FindVisualParent<T, R>(Visual? child) where T : Visual where R : Visual
        {
            while (true)
            {
                if (child == null) 
                    return default;

                Visual? parentObject = child.GetVisualParent();
                if (parentObject == null) 
                    return default;

                if (parentObject is T parent)
                    return parent;

                if (parentObject is R parent2) 
                    return parent2;

                child = parentObject;
            }
        }

        private static bool IsCopyKey(DragEventArgs e)
        {
            return e.KeyModifiers.HasFlag(platformCopyKeyModifier);
        }
    }
    
    public class DragAdorner : Control
    {
        public DragAdorner()
        {
            IsHitTestVisible = false;
        }
        
        private Rect drawRect;
        public void Update(ListBox listBox, IDropInfo dropInfo)
        {
            int indexOfDrop = dropInfo.InsertIndex;
            var container = listBox.ContainerFromIndex(indexOfDrop);

            if (container == null)
            {
                if (indexOfDrop == 0)
                    drawRect = new Rect(0, 0, listBox.Width, 1);
                else
                {
                    container = listBox.ContainerFromIndex(indexOfDrop - 1);
                    if (container != null)
                        drawRect = new Rect(container.Bounds.X, container.Bounds.Bottom, container.Bounds.Width, 1);
                    else
                        drawRect = new Rect(0, 0, 0, 1);
                }
            }
            else
            {
                if (dropInfo.InsertPosition.HasFlagFast(RelativeInsertPosition.TargetItemCenter)
                    && dropInfo.DropTargetAdorner == DropTargetAdorners.Highlight)
                    drawRect = new Rect(container.Bounds.X + 1, container.Bounds.Y, container.Bounds.Width - 2, container.Bounds.Height);
                else if (dropInfo.InsertPosition.HasFlagFast(RelativeInsertPosition.BeforeTargetItem))
                    drawRect = new Rect(container.Bounds.X, container.Bounds.Top, container.Bounds.Width, 1);
                else
                    drawRect = new Rect(container.Bounds.X, container.Bounds.Bottom, container.Bounds.Width, 1);
            }

            drawRect = new Rect(drawRect.X, drawRect.Y - listBox.Scroll!.Offset.Y, drawRect.Width, drawRect.Height);
            
            InvalidateVisual();
        }
        
        
        public void Update(TreeView treeView, ItemContainerGenerator itemContainerGenerator, IDropInfo dropInfo)
        {
            if (dropInfo.Effects == Common.Utils.DragDrop.DragDropEffects.None)
            {
                drawRect = new Rect();
                InvalidateVisual();
                return;
            }
            
            int indexOfDrop = dropInfo.InsertIndex;
            var container = itemContainerGenerator.ContainerFromIndex(indexOfDrop);

            if (container == null)
            {
                if (indexOfDrop == 0)
                    drawRect = new Rect(0, 0, treeView.Width, 1);
                else
                {
                    container = itemContainerGenerator.ContainerFromIndex(indexOfDrop - 1);
                    if (container != null)
                        drawRect = new Rect(container.Bounds.X, container.Bounds.Bottom, container.Bounds.Width, 1);
                    else
                        drawRect = new Rect(0, 0, 0, 1);
                }
            }
            else
            {
                double y = 0;
                Visual? parent = container.GetVisualParent();
                while (parent != null && !ReferenceEquals(parent, treeView))
                {
                    y += parent.Bounds.Y;
                    parent = parent.GetVisualParent();
                }
                
                var header = container.GetVisualChildren().FirstOrDefault()?.GetVisualChildren().FirstOrDefault();
                var height = header?.Bounds.Height ?? container.Bounds.Height;
                
                double top = container.TranslatePoint(new Point(0, 0), treeView)?.Y ?? 0;
                double bottom = container.TranslatePoint(new Point(0, height), treeView)?.Y ?? 0;
                
                if (dropInfo.InsertPosition.HasFlagFast(RelativeInsertPosition.TargetItemCenter)
                    && dropInfo.DropTargetAdorner == DropTargetAdorners.Highlight)
                    drawRect = new Rect(container.Bounds.X + 1, top, container.Bounds.Width - 2, container.Bounds.Height);
                else if (dropInfo.InsertPosition.HasFlagFast(RelativeInsertPosition.BeforeTargetItem))
                    drawRect = new Rect(container.Bounds.X, top, container.Bounds.Width, 1);
                else
                    drawRect = new Rect(container.Bounds.X, bottom, container.Bounds.Width, 1);
            }

            /*var scroll = treeView.FindDescendantOfType<ScrollViewer>();
            if (scroll != null)
                drawRect = new Rect(drawRect.X, drawRect.Y - scroll.Offset.Y, drawRect.Width, drawRect.Height);
*/
            InvalidateVisual();
        }
        
        public override void Render(DrawingContext context)
        {
            var adornered = AdornerLayer.GetAdornedElement(this);
            if (adornered == null)
                return;
            
            base.Render(context);
            context.DrawRectangle(null, new Pen(Brushes.Gray, 2), drawRect, 4);
        }
    }

    internal class AdornerHelper
    {
        public DragAdorner? Adorner;

        public void AddAdorner(Visual visual)
        {
            var layer = AdornerLayer.GetAdornerLayer(visual);
            if (layer is null)
                return;
            
            if (Adorner is { })
            {
                layer.Children.Remove(Adorner);
                Adorner = null;
            }

            Adorner = new DragAdorner
            {
                [AdornerLayer.AdornedElementProperty] = visual,
            };

            ((ISetLogicalParent) Adorner).SetParent(visual as ILogical);

            layer.Children.Add(Adorner);
        }

        public void RemoveAdorner(Visual visual)
        {
            var layer = AdornerLayer.GetAdornerLayer(visual);
            if (layer is { })
            {
                if (Adorner is { })
                {
                    layer.Children.Remove(Adorner);
                    ((ISetLogicalParent) Adorner).SetParent(null);
                    Adorner = null;
                }
            }
        }
    }
}
