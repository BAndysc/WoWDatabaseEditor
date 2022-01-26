using System;
using Avalonia;

namespace WDE.WorldMap.PanAndZoom
{
    public class ExtendedZoomBorder : ZoomBorder
    {
        public static readonly DirectProperty<ExtendedZoomBorder, Point> BottomRightProperty = AvaloniaProperty.RegisterDirect<ExtendedZoomBorder, Point>("BottomRight", o => o.BottomRight);
        
        public static readonly DirectProperty<ExtendedZoomBorder, Point> TopLeftProperty = AvaloniaProperty.RegisterDirect<ExtendedZoomBorder, Point>("TopLeft", o => o.TopLeft);

        public Point BottomRight => new((-OffsetX + Bounds.Width) / ZoomX, (-OffsetY + Bounds.Height) / ZoomY);

        public Point Center => new((-OffsetX + Bounds.Width / 2) / ZoomX, (-OffsetY + Bounds.Height / 2) / ZoomY);
        
        public Point TopLeft => new(-OffsetX / ZoomX, -OffsetY / ZoomY);


        static ExtendedZoomBorder()
        {
            OffsetXProperty.Changed.AddClassHandler<ExtendedZoomBorder>(UpdateTopBottomLeftRight);
            OffsetYProperty.Changed.AddClassHandler<ExtendedZoomBorder>(UpdateTopBottomLeftRight);
            ZoomXProperty.Changed.AddClassHandler<ExtendedZoomBorder>(UpdateTopBottomLeftRight);
            ZoomYProperty.Changed.AddClassHandler<ExtendedZoomBorder>(UpdateTopBottomLeftRight);
            BoundsProperty.Changed.AddClassHandler<ExtendedZoomBorder>(UpdateTopBottomLeftRight);
            MinWidthProperty.OverrideDefaultValue<ExtendedZoomBorder>(10);
            MinHeightProperty.OverrideDefaultValue<ExtendedZoomBorder>(10);
        }

        private static void UpdateTopBottomLeftRight(ExtendedZoomBorder arg1, AvaloniaPropertyChangedEventArgs arg2)
        {
            arg1.RaisePropertyChanged(BottomRightProperty, arg1.BottomRight, arg1.BottomRight);
            arg1.RaisePropertyChanged(TopLeftProperty, arg1.TopLeft, arg1.TopLeft);
        }
        
        public void SetCenter(double x, double y)
        {
            SetMatrix(new Matrix(ZoomX, 0, 0, ZoomY, -x * ZoomX + Bounds.Width / 2, -y * ZoomY + Bounds.Height / 2));
        }

        public void MoveToBounds(double left_, double right_, double top_, double bottom_)
        {
            double left = Math.Min(left_, right_);
            double right = Math.Max(left_, right_);
            double top = Math.Min(top_, bottom_);
            double bottom = Math.Max(top_, bottom_);
            var center = ((left + right) / 2, (top + bottom) / 2);

            var zoomX = -Bounds.Width / (left - right);
            var zoomY = -Bounds.Height / (top - bottom);

            var zoom = Math.Min(zoomX, zoomY);

            var offsetX = -zoom * center.Item1 + Bounds.Width / 2;
            var offsetY = -zoom * center.Item2 + Bounds.Height / 2;
            
            SetMatrix(new Matrix(zoom, 0, 0, zoom, offsetX, offsetY));
        }
    }
}