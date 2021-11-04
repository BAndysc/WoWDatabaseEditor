using System;

namespace WDE.WorldMap
{
    public class CoordsUtils
    {
        public static readonly double BlockSize = 533.333333333;
        public static readonly double TotalSize = BlockSize * 64;
        public static readonly double MinCoord = -TotalSize / 2;
        public static readonly double MaxCoord = TotalSize / 2;
        public static readonly double VirtualEditorSize = TotalSize;
        public static (int x, int y) Center = (0, 0);

        public static (double editorX, double editorY) WorldToEditor(double x, double y)
        {
            return ((Center.y - y) / (TotalSize) * VirtualEditorSize, (Center.x - x) / (TotalSize) * VirtualEditorSize);
        }

        public static (double x, double y) EditorToWorld(double editorX, double editorY)
        {
            return (Center.x - editorY / VirtualEditorSize * TotalSize,
                Center.y - editorX / VirtualEditorSize * TotalSize);
        }

        public static (int blockX, int blockY) WorldCoordsToBlock(double x, double y)
        {
            return ((int)Math.Floor(32 - (y / BlockSize)), (int)Math.Floor(32 - (x / BlockSize)));
        }

        public static (double x, double y) BlockToWorldCoords(int blockX, int blockY)
        {
            return ((32 - blockY) * BlockSize, (32 - blockX) * BlockSize);
        }
    }
}