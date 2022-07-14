using System.Collections.Generic;
using Avalonia;
using WDE.Common.Database;
using WDE.WorldMap.Models;

namespace WDE.PathPreviewTool.ViewModels;

public class PathViewModel : IMapItem
{
    public readonly IReadOnlyList<IWaypoint> Waypoints;

    public PathViewModel(ICreature creature, IReadOnlyList<IWaypoint> waypoints)
    {
        Waypoints = waypoints;
        X = creature.X;
        Y = creature.Y;
        Z = creature.Z;
    }

    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public Rect VirtualBounds { get; set; }
}