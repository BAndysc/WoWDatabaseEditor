using System;
using System.Windows;
using CommonDrag = WDE.Common.Utils.DragDrop;
using GongDrag = GongSolutions.Wpf.DragDrop;
using DragDropEffects = System.Windows.DragDropEffects;

namespace WDE.Common.WPF.Attached
{
    public static class GongDragAndDrop
    {
        public static readonly DependencyProperty WdeDropHandlerProperty = DependencyProperty.RegisterAttached("WdeDropHandler",
            typeof(CommonDrag.IDropTarget),
            typeof(GongDragAndDrop),
            new FrameworkPropertyMetadata(null, OnWdeDropHandlerChanged));

        public static object GetWdeDropHandler(DependencyObject d)
        {
            return d.GetValue(WdeDropHandlerProperty);
        }

        public static void SetWdeDropHandler(DependencyObject d, object value)
        {
            d.SetValue(WdeDropHandlerProperty, value);
        }

        private static void OnWdeDropHandlerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var adapter = new DragTargetAdapter((CommonDrag.IDropTarget)e.NewValue);
            d.SetValue(GongDrag.DragDrop.DropHandlerProperty, adapter);
        }
        
        internal static CommonDrag.RelativeInsertPosition ToCommon(this GongDrag.RelativeInsertPosition position)
        {
            CommonDrag.RelativeInsertPosition dest = CommonDrag.RelativeInsertPosition.None;
            if (position.HasFlag(GongDrag.RelativeInsertPosition.AfterTargetItem))
                dest |= CommonDrag.RelativeInsertPosition.AfterTargetItem;
            if (position.HasFlag(GongDrag.RelativeInsertPosition.BeforeTargetItem))
                dest |= CommonDrag.RelativeInsertPosition.BeforeTargetItem;
            if (position.HasFlag(GongDrag.RelativeInsertPosition.TargetItemCenter))
                dest |= CommonDrag.RelativeInsertPosition.TargetItemCenter;

            return dest;
        }
        
        internal static GongDrag.RelativeInsertPosition ToGong(this CommonDrag.RelativeInsertPosition position)
        {
            GongDrag.RelativeInsertPosition dest = GongDrag.RelativeInsertPosition.None;
            if (position.HasFlag(CommonDrag.RelativeInsertPosition.AfterTargetItem))
                dest |= GongDrag.RelativeInsertPosition.AfterTargetItem;
            if (position.HasFlag(CommonDrag.RelativeInsertPosition.BeforeTargetItem))
                dest |= GongDrag.RelativeInsertPosition.BeforeTargetItem;
            if (position.HasFlag(CommonDrag.RelativeInsertPosition.TargetItemCenter))
                dest |= GongDrag.RelativeInsertPosition.TargetItemCenter;

            return dest;
        }

        internal static Type ToGong(this CommonDrag.DropTargetAdorners adorner)
        {
            switch (adorner)
            {
                case CommonDrag.DropTargetAdorners.Highlight:
                    return GongDrag.DropTargetAdorners.Highlight;
                case CommonDrag.DropTargetAdorners.Insert:
                    return GongDrag.DropTargetAdorners.Insert;
                default:
                    throw new ArgumentOutOfRangeException(nameof(adorner), adorner, null);
            }
        }

        internal static CommonDrag.DragDropEffects ToCommon(this System.Windows.DragDropEffects effect)
        {
            switch (effect)
            {
                case DragDropEffects.Scroll:
                    return Common.Utils.DragDrop.DragDropEffects.Scroll;
                case DragDropEffects.All:
                    return Common.Utils.DragDrop.DragDropEffects.All;
                case DragDropEffects.None:
                    return Common.Utils.DragDrop.DragDropEffects.None;
                case DragDropEffects.Copy:
                    return Common.Utils.DragDrop.DragDropEffects.Copy;
                case DragDropEffects.Move:
                    return Common.Utils.DragDrop.DragDropEffects.Move;
                case DragDropEffects.Link:
                    return Common.Utils.DragDrop.DragDropEffects.Link;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        internal static DragDropEffects ToWindows(this CommonDrag.DragDropEffects effect)
        {
            switch (effect)
            {
                case Common.Utils.DragDrop.DragDropEffects.Scroll:
                    return DragDropEffects.Scroll;
                case Common.Utils.DragDrop.DragDropEffects.All:
                    return DragDropEffects.All;
                case Common.Utils.DragDrop.DragDropEffects.None:
                    return DragDropEffects.None;
                case Common.Utils.DragDrop.DragDropEffects.Copy:
                    return DragDropEffects.Copy;
                case Common.Utils.DragDrop.DragDropEffects.Move:
                    return DragDropEffects.Move;
                case Common.Utils.DragDrop.DragDropEffects.Link:
                    return DragDropEffects.Link;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        private class DragTargetAdapter : GongDrag.IDropTarget
        {
            private readonly CommonDrag.IDropTarget target;

            public DragTargetAdapter(CommonDrag.IDropTarget target)
            {
                this.target = target;
            }
            
            public void DragOver(GongDrag.IDropInfo dropInfo)
            {
                var adapter = new DropInfoAdapter(dropInfo);
                target.DragOver(adapter);
                dropInfo.Effects = adapter.Effects.ToWindows();
                dropInfo.DropTargetAdorner = adapter.DropTargetAdorner.ToGong();
            }

            public void Drop(GongDrag.IDropInfo dropInfo)
            {
                var adapter = new DropInfoAdapter(dropInfo);
                target.Drop(adapter);
            }
        }

        private class DropInfoAdapter : CommonDrag.IDropInfo
        {
            public DropInfoAdapter(GongDrag.IDropInfo dropInfo)
            {
                Data = dropInfo.Data;
                TargetItem = dropInfo.TargetItem;
                InsertIndex = dropInfo.InsertIndex;
                if (dropInfo.DropTargetAdorner == GongDrag.DropTargetAdorners.Highlight)
                    DropTargetAdorner = CommonDrag.DropTargetAdorners.Highlight;
                else if (dropInfo.DropTargetAdorner == GongDrag.DropTargetAdorners.Insert)
                    DropTargetAdorner = CommonDrag.DropTargetAdorners.Insert;
                Effects = dropInfo.Effects.ToCommon();
                InsertPosition = dropInfo.InsertPosition.ToCommon();
            }
            
            public object? Data { get; internal set; }
            public object? TargetItem { get; internal set; }
            public int InsertIndex { get; internal set; }
            public CommonDrag.DropTargetAdorners DropTargetAdorner { get; set; }
            public CommonDrag.DragDropEffects Effects { get; set; }
            public CommonDrag.RelativeInsertPosition InsertPosition { get; internal set; }
        }
    }
}