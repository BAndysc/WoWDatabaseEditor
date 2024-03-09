using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace WDE.Common.Avalonia.Utils;

public static class DrawingExtensions
{
    // draws an arrow from point p1 to point p2, which has a triangle on the left side
    // so that the line looks like this >---<
    public static void DrawArrow(this DrawingContext ctx, IPen pen, Point p1, Point p2, float arrowSize)
    {
        var dir = new Vector((p2 - p1).X, (p2 - p1).Y);
        dir /= dir.Length;

        var perp = new Vector(-dir.Y, dir.X);

        var triangle = new PolylineGeometry() { IsFilled = true };
        triangle.Points.Add(p1 + dir * arrowSize);
        triangle.Points.Add(p1 + perp * arrowSize / 2);
        triangle.Points.Add(p1 - perp * arrowSize / 2);
        ctx.DrawGeometry(pen.Brush, null, triangle);
        
        ctx.DrawLine(pen, p1 + dir * arrowSize / 2, p2 - dir * arrowSize / 2);
        
        triangle = new PolylineGeometry() { IsFilled = true };
        triangle.Points.Add(p2 - dir * arrowSize);
        triangle.Points.Add(p2 + perp * arrowSize / 2);
        triangle.Points.Add(p2 - perp * arrowSize / 2);
        ctx.DrawGeometry(pen.Brush, null, triangle);
    }
}