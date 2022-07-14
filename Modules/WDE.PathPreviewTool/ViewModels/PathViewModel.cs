using Avalonia;
using WDE.WorldMap.Models;

namespace WDE.PathPreviewTool.ViewModels;

public class PathViewModel : IMapItem
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public Rect VirtualBounds { get; set; }
}