using System;

namespace WDE.Common.Utils.DragDrop
{
    [Flags]
    public enum RelativeInsertPosition
    {
        None = 0,
        BeforeTargetItem = 1,
        AfterTargetItem = 2,
        TargetItemCenter = 4,
    }

    public interface IDropTarget
    {
        void DragOver(IDropInfo dropInfo);
        void Drop(IDropInfo dropInfo);
    }

    public interface IDragSource
    {
        bool CanDrag(object data);
    }

    public enum DragDropEffects
    {
        /// <summary>
        /// A drop would not be allowed. 
        /// </summary>
        None = 0,
        /// <summary>
        /// A copy operation would be performed.
        /// </summary>
        Copy = 1,
        /// <summary>
        /// A move operation would be performed.
        /// </summary>
        Move = 2,
        /// <summary>
        /// A link from the dropped data to the original data would be established.
        /// </summary>
        Link = 4,
        /// <summary>
        /// A drag scroll operation is about to occur or is occurring in the target. 
        /// </summary>
        Scroll = unchecked((int)0x80000000),
        /// <summary>
        /// All operation is about to occur data is copied or removed from the drag source, and
        /// scrolled in the drop target. 
        /// </summary>
        All = Copy | Move | Scroll,
    }

    public enum DropTargetAdorners
    {
        Highlight,
        Insert
    }

    public interface IDropInfo
    {
        object Data { get; }
        object? TargetItem { get; }
        int InsertIndex { get; }
        bool IsCopy { get; }
        DropTargetAdorners DropTargetAdorner { get; set; }
        DragDropEffects Effects { get; set; }
        RelativeInsertPosition InsertPosition { get; set; }
    }
}