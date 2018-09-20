using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WDE.Blueprints.GeminiGraphEditor
{
    public class BezierLine : Shape
    {
        private const FrameworkPropertyMetadataOptions MetadataOptions =
            FrameworkPropertyMetadataOptions.AffectsMeasure | 
            FrameworkPropertyMetadataOptions.AffectsRender;

        private Geometry _geometry;

        public static readonly DependencyProperty X1Property = DependencyProperty.Register(
            "X1", typeof(double), typeof(BezierLine),
            new FrameworkPropertyMetadata(0.0, MetadataOptions));

        public double X1
        {
            get { return (double) GetValue(X1Property); }
            set { SetValue(X1Property, value); }
        }

        public static readonly DependencyProperty X2Property = DependencyProperty.Register(
            "X2", typeof(double), typeof(BezierLine),
            new FrameworkPropertyMetadata(0.0, MetadataOptions));

        public double X2
        {
            get { return (double) GetValue(X2Property); }
            set { SetValue(X2Property, value); }
        }

        public static readonly DependencyProperty Y1Property = DependencyProperty.Register(
            "Y1", typeof(double), typeof(BezierLine),
            new FrameworkPropertyMetadata(0.0, MetadataOptions));

        public double Y1
        {
            get { return (double) GetValue(Y1Property); }
            set { SetValue(Y1Property, value); }
        }

        public static readonly DependencyProperty Y2Property = DependencyProperty.Register(
            "Y2", typeof(double), typeof(BezierLine),
            new FrameworkPropertyMetadata(0.0, MetadataOptions));

        public double Y2
        {
            get { return (double) GetValue(Y2Property); }
            set { SetValue(Y2Property, value); }
        }

        protected override Geometry DefiningGeometry
        {
            get { return _geometry; }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            var rx1 = X1;// + 15;
            var rx2 = X2;// - 15;
            var midX = rx1 + ((rx2 - rx1) / 2);
            var midY = Y1 + ((Y2 - Y1) / 2);

            PathSegmentCollection segments;

            if (rx1 > rx2 - 30)
            {
                double diff = Math.Max((rx1 - rx2) / 3, 70);
                segments = new PathSegmentCollection
                {
                    new BezierSegment
                    {
                        Point1 = new Point(rx1 + diff, Y1),
                        Point2 = new Point(midX, midY),
                        Point3 = new Point(midX, midY),
                        IsStroked = true
                    },
                    new BezierSegment
                    {
                        Point1 = new Point(midX, midY),
                        Point2 = new Point(rx2-diff, Y2),
                        Point3 = new Point(rx2, Y2),
                        IsStroked = true
                    },
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
                        Point3 = new Point(rx2, Y2),
                        IsStroked = true
                    }
                };
            }

            _geometry = new PathGeometry
            {
                Figures =
                {
                    new PathFigure
                    {
                        IsFilled = false,
                        StartPoint = new Point(rx1, Y1),
                        Segments = segments
                    }
                }
            };

            return base.MeasureOverride(constraint);
        }
    }
}
