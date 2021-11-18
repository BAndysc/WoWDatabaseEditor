using Avalonia;

namespace WDE.WorldMap.Models
{
    public interface IMapItem
    {
        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public Rect VirtualBounds { get; set; }
    }
}