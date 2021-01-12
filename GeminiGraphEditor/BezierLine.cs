using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GeminiGraphEditor
{
    public class BezierLine : Shape
    {
        private const FrameworkPropertyMetadataOptions MetadataOptions =
            FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender;

        public static readonly DependencyProperty X1Property = DependencyProperty.Register("X1",
            typeof(double),
            typeof(BezierLine),
            new FrameworkPropertyMetadata(0.0, MetadataOptions));

        public static readonly DependencyProperty X2Property = DependencyProperty.Register("X2",
            typeof(double),
            typeof(BezierLine),
            new FrameworkPropertyMetadata(0.0, MetadataOptions));

        public static readonly DependencyProperty Y1Property = DependencyProperty.Register("Y1",
            typeof(double),
            typeof(BezierLine),
            new FrameworkPropertyMetadata(0.0, MetadataOptions));

        public static readonly DependencyProperty Y2Property = DependencyProperty.Register("Y2",
            typeof(double),
            typeof(BezierLine),
            new FrameworkPropertyMetadata(0.0, MetadataOptions));

        private Geometry geometry;

        public double X1
        {
            get => (double) GetValue(X1Property);
            set => SetValue(X1Property, value);
        }

        public double X2
        {
            get => (double) GetValue(X2Property);
            set => SetValue(X2Property, value);
        }

        public double Y1
        {
            get => (double) GetValue(Y1Property);
            set => SetValue(Y1Property, value);
        }

        public double Y2
        {
            get => (double) GetValue(Y2Property);
            set => SetValue(Y2Property, value);
        }

        protected override Geometry DefiningGeometry => geometry;

        protected virtual PathSegmentCollection GetSegments()
        {
            PathSegmentCollection segments;

            double midX = X1 + (X2 - X1) / 2;
            double midY = Y1 + (Y2 - Y1) / 2;

            if (X1 > X2 - 30)
            {
                double diff = Math.Max((X1 - X2) / 3, 70);
                segments = new PathSegmentCollection
                {
                    new BezierSegment
                    {
                        Point1 = new Point(X1 + diff, Y1),
                        Point2 = new Point(midX, midY),
                        Point3 = new Point(midX, midY),
                        IsStroked = true
                    },
                    new BezierSegment
                    {
                        Point1 = new Point(midX, midY),
                        Point2 = new Point(X2 - diff, Y2),
                        Point3 = new Point(X1, Y2),
                        IsStroked = true
                    }
                };
            }
            else
            {
                segments = new PathSegmentCollection
                {
                    new BezierSegment
                    {
                        Point1 = new Point(midX, Y1),
                        Point2 = new Point(midX, Y2),
                        Point3 = new Point(X2, Y2),
                        IsStroked = true
                    }
                };
            }

            return segments;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            PathSegmentCollection segments = GetSegments();

            geometry = new PathGeometry
            {
                Figures =
                {
                    new PathFigure
                    {
                        IsFilled = false,
                        StartPoint = new Point(X1, Y1),
                        Segments = segments
                    }
                }
            };

            return base.MeasureOverride(constraint);
        }
    }

    public class VerticalBezierLine : BezierLine
    {
        protected override PathSegmentCollection GetSegments()
        {
            double midX = X1 + (X2 - X1) / 2;
            double midY = Y1 + (Y2 - Y1) / 2;

            return new PathSegmentCollection
            {
                new BezierSegment
                {
                    Point1 = new Point(X1, Y1 + 30),
                    Point2 = new Point(midX, midY),
                    Point3 = new Point(midX, midY),
                    IsStroked = true
                },
                new BezierSegment
                {
                    Point1 = new Point(midX, midY),
                    Point2 = new Point(X2, Y2 - 30),
                    Point3 = new Point(X2, Y2),
                    IsStroked = true
                }
            };
        }
    }
}