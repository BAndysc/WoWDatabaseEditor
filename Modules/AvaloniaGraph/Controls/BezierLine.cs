using System;
using Avalonia;
using Avalonia.Controls.Shapes;
using Avalonia.Media;

namespace AvaloniaGraph.Controls;

public class BezierLine : Shape //, ICustomHitTest
{
    public static readonly StyledProperty<Point> StartPointProperty =
        AvaloniaProperty.Register<BezierLine, Point>(nameof(StartPoint));

    public static readonly StyledProperty<Point> EndPointProperty =
        AvaloniaProperty.Register<BezierLine, Point>(nameof(StartPoint));

    static BezierLine()
    {
        StrokeThicknessProperty.OverrideDefaultValue<BezierLine>(1.0);
        StrokeProperty.OverrideDefaultValue<BezierLine>(Brushes.Red);
        AffectsGeometry<BezierLine>(StartPointProperty, EndPointProperty);
    }

    public bool Relative { get; set; } = true;

    public Point StartPoint
    {
        get => GetValue(StartPointProperty);
        set => SetValue(StartPointProperty, value);
    }

    public Point RelativeEndPoint => EndPoint - StartPoint;

    public Point EndPoint
    {
        get => GetValue(EndPointProperty);
        set => SetValue(EndPointProperty, value);
    }

    protected virtual PathSegments GetSegments()
    {
        var midX = StartPoint.X + (EndPoint.X - StartPoint.X) / 2;
        var midY = StartPoint.Y + (EndPoint.Y - StartPoint.Y) / 2;

        if (StartPoint.X > EndPoint.X - 30)
        {
            var diff = Math.Max((StartPoint.X - EndPoint.X) / 3, 70);
            return new PathSegments
            {
                new BezierSegment
                {
                    Point1 = new Point(StartPoint.X + diff, StartPoint.Y),
                    Point2 = new Point(midX, midY),
                    Point3 = new Point(midX, midY)
                },
                new BezierSegment
                {
                    Point1 = new Point(midX, midY),
                    Point2 = new Point(EndPoint.X - diff, EndPoint.Y),
                    Point3 = new Point(StartPoint.X, EndPoint.Y)
                }
            };
        }

        return new PathSegments
        {
            new BezierSegment
            {
                Point1 = new Point(midX, StartPoint.Y),
                Point2 = new Point(midX, EndPoint.Y),
                Point3 = EndPoint
            }
        };
    }

    protected override Geometry? CreateDefiningGeometry()
    {
        return new PathGeometry
        {
            Figures =
            {
                new PathFigure
                {
                    IsFilled = false,
                    StartPoint = Relative
                        ? StartPoint - new Point(Math.Min(StartPoint.X, EndPoint.X), Math.Min(StartPoint.Y, EndPoint.Y))
                        : StartPoint,
                    Segments = GetSegments(),
                    IsClosed = false
                }
            }
        };
    }

    public bool HitTest(Point point)
    {
        return false;
    }
}

public class VerticalBezierLine : BezierLine
{
    protected override PathSegments GetSegments()
    {
        var start = StartPoint;
        var end = EndPoint;
        var topleft = new Point(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y));
        var bottomRight = new Point(Math.Max(start.X, end.X), Math.Max(start.Y, end.Y));

        if (Relative)
        {
            start -= topleft;
            end -= topleft;
        }

        //start = topleft - topleft;
        //end = bottomRight - topleft;

        var midX = (bottomRight.X - topleft.X) / 2; // start.X + 
        var midY = (bottomRight.Y - topleft.Y) / 2; // start.Y + 

        return new PathSegments
        {
            new BezierSegment
            {
                Point1 = new Point(start.X, start.Y + 30),
                Point2 = new Point(midX, midY),
                Point3 = new Point(midX, midY)
            },
            new BezierSegment
            {
                Point1 = new Point(midX, midY),
                Point2 = new Point(end.X, end.Y - 30),
                Point3 = end
            }
        };
    }
}


public class UniversalBezierLine : BezierLine
{
    public static readonly StyledProperty<ConnectorAttachMode> StartModeProperty =
        AvaloniaProperty.Register<UniversalBezierLine, ConnectorAttachMode>(nameof(StartMode));

    public static readonly StyledProperty<ConnectorAttachMode> EndModeProperty =
        AvaloniaProperty.Register<UniversalBezierLine, ConnectorAttachMode>(nameof(EndMode));

    public ConnectorAttachMode StartMode
    {
        get => GetValue(StartModeProperty);
        set => SetValue(StartModeProperty, value);
    }
    
    public ConnectorAttachMode EndMode
    {
        get => GetValue(EndModeProperty);
        set => SetValue(EndModeProperty, value);
    }

    private static Point GetOffset(ConnectorAttachMode mode)
    {
        return mode switch
        {
            ConnectorAttachMode.Top => new Point(0, -30),
            ConnectorAttachMode.Bottom => new Point(0, 30),
            ConnectorAttachMode.Middle => new Point(0, 0),
            ConnectorAttachMode.Left => new Point(-30, 0),
            ConnectorAttachMode.Right => new Point(30, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }

    protected override PathSegments GetSegments()
    {
        var start = StartPoint;
        var end = EndPoint;
        var topleft = new Point(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y));
        var bottomRight = new Point(Math.Max(start.X, end.X), Math.Max(start.Y, end.Y));

        if (Relative)
        {
            start -= topleft;
            end -= topleft;
        }

        //start = topleft - topleft;
        //end = bottomRight - topleft;

        var midX = (bottomRight.X - topleft.X) / 2; // start.X + 
        var midY = (bottomRight.Y - topleft.Y) / 2; // start.Y + 

        return new PathSegments
        {
            new BezierSegment
            {
                Point1 = new Point(start.X, start.Y) + GetOffset(StartMode),
                Point2 = new Point(midX, midY),
                Point3 = new Point(midX, midY)
            },
            new BezierSegment
            {
                Point1 = new Point(midX, midY),
                Point2 = new Point(end.X, end.Y) + GetOffset(EndMode),
                Point3 = end 
            }
        };
    }
}