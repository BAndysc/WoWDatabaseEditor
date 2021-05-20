using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using WDE.Common.Utils.DragDrop;
using DragDropEffects = Avalonia.Input.DragDropEffects;

namespace WDE.Common.Avalonia.DnD
{
    public class DragAndDrop
    {
        public static readonly AttachedProperty<bool> IsDropTargetProperty = AvaloniaProperty.RegisterAttached<IAvaloniaObject, bool>("IsDropTarget", typeof(DragAndDrop));
        public static readonly AttachedProperty<bool> IsDragSourceProperty = AvaloniaProperty.RegisterAttached<IAvaloniaObject, bool>("IsDragSource", typeof(DragAndDrop));
        public static readonly AttachedProperty<IDropTarget?> DropHandlerProperty = AvaloniaProperty.RegisterAttached<IAvaloniaObject, IDropTarget?>("DropHandler", typeof(DragAndDrop));

        public static bool GetIsDropTarget(IAvaloniaObject obj)
        {
            return (bool) obj.GetValue(IsDropTargetProperty);
        }

        public static void SetIsDropTarget(IAvaloniaObject obj, bool value)
        {
            obj.SetValue(IsDropTargetProperty, value);
        }

        public static bool GetIsDragSource(IAvaloniaObject obj)
        {
            return (bool) obj.GetValue(IsDragSourceProperty);
        }

        public static void SetIsDragSource(IAvaloniaObject obj, bool value)
        {
            obj.SetValue(IsDragSourceProperty, value);
        }

        public static IDropTarget? GetDropHandler(IAvaloniaObject obj)
        {
            return (IDropTarget?) obj.GetValue(DropHandlerProperty);
        }

        public static void SetDropHandler(IAvaloniaObject obj, IDropTarget? value)
        {
            obj.SetValue(DropHandlerProperty, value);
        }
        
        private static Point m_cursorStartPos;
        private static AdornerHelper adorner;

        static DragAndDrop()
        {
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
            
            TreeViewItem? targetItem = FindVisualParent<TreeViewItem>((IVisual?)e.Source);
            if (targetItem == null || targetItem.DataContext == null) 
                return;
            
            dragging = true;
            var data = new DataObject();
            data.Set("", new DragInfo()
            {
                draggedElement = new List<object?>(){treeView.SelectedItem},
                draggedIndex = new List<int>(){0}
            });

            DoDrag(e, treeView, data);
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
            
            ListBoxItem? targetItem = FindVisualParent<ListBoxItem>((IVisual?)e.Source);
            if (targetItem == null || targetItem.DataContext == null) 
                return;
            
            dragging = true;
            var data = new DataObject();
            data.Set("", new DragInfo()
            {
                draggedElement = listBox.Selection.SelectedItems,
                draggedIndex = listBox.Selection.SelectedIndexes
            });
                    
            DoDrag(e, listBox, data);
        }

        private static async Task DoDrag(PointerEventArgs e, IVisual listBox, DataObject data)
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
        }

        private static void OnListDragOver(object? sender, DragEventArgs e)
        {
            var listBox = sender as ListBox;
            var dropElement = FindVisualParent<ListBoxItem>(e.Source as IVisual);
            var dragInfo = e.Data.Get("") as DragInfo?;
            if (dragInfo == null || dragInfo.Value.draggedElement.Count == 0)
                return;
            
            if (listBox == null)
                return;

            var dropHandler = GetDropHandler(listBox);
            if (dropHandler == null)
                return;
            
            var indexOfDrop = listBox.ItemContainerGenerator.IndexFromContainer(dropElement);
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
                TargetItem = listBox.Items
            };
            dropHandler.DragOver(dropInfo);

            {
                double mousePosY = e.GetPosition(listBox).Y;
                if (mousePosY < 10)
                {
                    listBox.Scroll.Offset = listBox.Scroll.Offset + new Vector(0, -1);
                }
                else if (mousePosY > listBox.Bounds.Height - 10)
                {
                    listBox.Scroll.Offset = listBox.Scroll.Offset + new Vector(0, +1);
                }
            }
            
            adorner.Adorner?.Update(listBox, dropInfo);
        }
        
        private static void OnListDrop(object? sender, DragEventArgs e)
        {
            var listBox = sender as ListBox;
            var dropElement = FindVisualParent<ListBoxItem>(e.Source as IVisual);
            var dragInfo = e.Data.Get("") as DragInfo?;
            if (dragInfo == null || dragInfo.Value.draggedElement.Count == 0)
                return;
            
            if (listBox == null)
                return;
            
            var dropHandler = GetDropHandler(listBox);
            if (dropHandler == null)
                return;
            
            adorner.RemoveAdorner(listBox);
            
            var indexOfDrop = listBox.ItemContainerGenerator.IndexFromContainer(dropElement);
            if (dropElement != null)
            {
                var pos = e.GetPosition(dropElement);
                if (pos.Y > dropElement.Bounds.Height / 2)
                    indexOfDrop++;
            }
            else
                indexOfDrop = listBox.ItemCount;
            
            dropHandler.Drop(new DropInfo(dragInfo.Value.draggedElement[0]!)
            {
                InsertIndex = indexOfDrop,
                TargetItem = listBox.Items
            });
        }
        
        
        
        
        

        private static void OnTreeViewDragOver(object? sender, DragEventArgs e)
        {
            var treeView = sender as TreeView;
            var dropElement = FindVisualParent<TreeViewItem>(e.Source as IVisual);
            var dragInfo = e.Data.Get("") as DragInfo?;
            if (dragInfo == null || dragInfo.Value.draggedElement.Count == 0)
                return;
            
            if (treeView == null)
                return;

            var dropHandler = GetDropHandler(treeView);
            if (dropHandler == null)
                return;

            var parent = dropElement == null ? treeView : FindVisualParent<TreeView, TreeViewItem>(dropElement);
            
            ITreeItemContainerGenerator treeItemContainerGenerator;
            if (parent is TreeView tv)
                treeItemContainerGenerator = tv.ItemContainerGenerator;
            else if (parent is TreeViewItem ti)
                treeItemContainerGenerator = ti.ItemContainerGenerator;
            else
                return;
            
            adorner.AddAdorner(treeView); // parent
            var indexOfDrop = treeItemContainerGenerator.IndexFromContainer(dropElement);
            RelativeInsertPosition insertPosition = RelativeInsertPosition.None;
            
            if (dropElement != null)
            {
                var header = dropElement.GetVisualChildren().FirstOrDefault().GetVisualChildren().FirstOrDefault();
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

            if (insertPosition.HasFlag(RelativeInsertPosition.AfterTargetItem) &&
                (dropElement?.IsExpanded ?? false) &&
                dropElement.ItemCount > 0)
            {
                indexOfDrop = 0;
                insertPosition = RelativeInsertPosition.BeforeTargetItem;
                dropElement = (TreeViewItem) dropElement.ItemContainerGenerator.ContainerFromIndex(0);
            }
            
            dropInfo = new DropInfo(dragInfo.Value.draggedElement[0]!)
            {
                InsertIndex = indexOfDrop,
                InsertPosition = insertPosition,
                TargetItem = ((IControl?)dropElement)?.DataContext
            };
            dropHandler.DragOver(dropInfo);

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
            
            Console.WriteLine("Over " + dropElement + " (" + dropElement?.DataContext?.ToString() + ")");
            Console.WriteLine("Best parent: " + parent + " (" + ((IControl?)parent)?.DataContext?.ToString() + ")");
            Console.WriteLine("Index: " + indexOfDrop + " insert position " + insertPosition.ToString());

            if (dropInfo.DropTargetAdorner == DropTargetAdorners.Insert)
                dropInfo.InsertPosition =
                    (RelativeInsertPosition) ((int) dropInfo.InsertPosition &
                                              ~(int) RelativeInsertPosition.TargetItemCenter);
            adorner.Adorner?.Update(treeView, treeItemContainerGenerator, dropInfo);
        }

        private static IDropInfo? dropInfo;
        
        private static void OnTreeViewDrop(object? sender, DragEventArgs e)
        {
            var treeView = sender as TreeView;
            var dropElement = FindVisualParent<TreeViewItem>(e.Source as IVisual);
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
            
            if (dropInfo != null)
                dropHandler.Drop(dropInfo);
            dropInfo = null;
            
            Dispatcher.UIThread.Post(() =>
            {
                if (previousOffset.HasValue)
                    scrollViewer!.Offset = previousOffset.Value;
            }, DispatcherPriority.Render);
        }


        private static T? FindVisualParent<T>(IVisual? child) where T : IVisual
        {
            while (true)
            {
                if (child == null) 
                    return default;

                IVisual parentObject = child.VisualParent;
                if (parentObject == null) 
                    return default;

                if (parentObject is T parent) 
                    return parent;

                child = parentObject;
            }
        }

        private static IVisual? FindVisualParent<T, R>(IVisual? child) where T : IVisual where R : IVisual
        {
            while (true)
            {
                if (child == null) 
                    return default;

                IVisual parentObject = child.VisualParent;
                if (parentObject == null) 
                    return default;

                if (parentObject is T parent)
                    return parent;

                if (parentObject is R parent2) 
                    return parent2;

                child = parentObject;
            }
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
            var container = listBox.ItemContainerGenerator.ContainerFromIndex(indexOfDrop);

            if (container == null)
            {
                if (indexOfDrop == 0)
                    drawRect = new Rect(0, 0, listBox.Width, 1);
                else
                {
                    container = listBox.ItemContainerGenerator.ContainerFromIndex(indexOfDrop - 1);
                    if (container != null)
                        drawRect = new Rect(container.Bounds.X, container.Bounds.Bottom, container.Bounds.Width, 1);
                    else
                        drawRect = new Rect(0, 0, 0, 1);
                }
            }
            else
            {
                if (dropInfo.InsertPosition.HasFlag(RelativeInsertPosition.TargetItemCenter)
                    && dropInfo.DropTargetAdorner == DropTargetAdorners.Highlight)
                    drawRect = new Rect(container.Bounds.X + 1, container.Bounds.Y, container.Bounds.Width - 2, container.Bounds.Height);
                else if (dropInfo.InsertPosition.HasFlag(RelativeInsertPosition.BeforeTargetItem))
                    drawRect = new Rect(container.Bounds.X, container.Bounds.Top, container.Bounds.Width, 1);
                else
                    drawRect = new Rect(container.Bounds.X, container.Bounds.Bottom, container.Bounds.Width, 1);
            }

            drawRect = new Rect(drawRect.X, drawRect.Y - listBox.Scroll.Offset.Y, drawRect.Width, drawRect.Height);
            
            InvalidateVisual();
        }
        
        
        public void Update(TreeView treeView, ITreeItemContainerGenerator itemContainerGenerator, IDropInfo dropInfo)
        {
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
                IVisual parent = container.VisualParent;
                while (parent != null && parent != treeView)
                {
                    y += parent.Bounds.Y;
                    parent = parent.VisualParent;
                }
                
                var header = container.GetVisualChildren().FirstOrDefault().GetVisualChildren().FirstOrDefault();
                var height = header?.Bounds.Height ?? container.Bounds.Height;
                
                double top = container.TranslatePoint(new Point(0, 0), treeView)?.Y ?? 0;
                double bottom = container.TranslatePoint(new Point(0, height), treeView)?.Y ?? 0;
                
                if (dropInfo.InsertPosition.HasFlag(RelativeInsertPosition.TargetItemCenter)
                    && dropInfo.DropTargetAdorner == DropTargetAdorners.Highlight)
                    drawRect = new Rect(container.Bounds.X + 1, top, container.Bounds.Width - 2, container.Bounds.Height);
                else if (dropInfo.InsertPosition.HasFlag(RelativeInsertPosition.BeforeTargetItem))
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

        public void AddAdorner(IVisual visual)
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

        public void RemoveAdorner(IVisual visual)
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