using Avalonia;
using static System.Math;

namespace WDE.WorldMap.PanAndZoom
{
    /// <summary>
    /// Zoom helper methods.
    /// </summary>
    public static class ZoomHelper
    {
        /// <summary>
        /// Calculate pan and zoom matrix based on provided streatch mode.
        /// </summary>
        /// <param name="panelWidth">The panel width.</param>
        /// <param name="panelHeight">The panel height.</param>
        /// <param name="elementWidth">The element width.</param>
        /// <param name="elementHeight">The element height.</param>
        /// <param name="mode">The stretch mode.</param>
        public static Matrix CalculateMatrix(double panelWidth, double panelHeight, double elementWidth, double elementHeight, StretchMode mode)
        {
            var zx = panelWidth / elementWidth;
            var zy = panelHeight / elementHeight;
            var cx = elementWidth / 2.0;
            var cy = elementHeight / 2.0;

            switch (mode)
            {
                default:
                case StretchMode.None:
                    return Matrix.Identity;
                case StretchMode.Fill:
                    return MatrixHelper.ScaleAt(zx, zy, cx, cy);
                case StretchMode.Uniform:
                    {
                        var zoom = Min(zx, zy);
                        return MatrixHelper.ScaleAt(zoom, zoom, cx, cy);
                    }
                case StretchMode.UniformToFill:
                    {
                        var zoom = Max(zx, zy);
                        return MatrixHelper.ScaleAt(zoom, zoom, cx, cy);
                    }
            }
        }

        /// <summary>
        /// Calculate scrollable properties.
        /// </summary>
        /// <param name="source">The source bounds.</param>
        /// <param name="matrix">The transform matrix.</param>
        /// <param name="extent">The extent of the scrollable content.</param>
        /// <param name="viewport">The size of the viewport.</param>
        /// <param name="offset">The current scroll offset.</param>
        public static void CalculateScrollable(Rect source, Matrix matrix, out Size extent, out Size viewport, out Vector offset)
        {
            var bounds = new Rect(0, 0, source.Width, source.Height);
            
            viewport = bounds.Size;

            var transformed = bounds.TransformToAABB(matrix);

            ZoomBorder.Log($"[CalculateScrollable] source: {source}, bounds: {bounds}, transformed: {transformed}");

            var width = transformed.Size.Width;
            var height = transformed.Size.Height;

            if (width < viewport.Width)
            {
                width = viewport.Width;

                if (transformed.Position.X < 0.0)
                {
                    width += Abs(transformed.Position.X);
                }
                else
                {
                    var widthTranslated = transformed.Size.Width + transformed.Position.X;
                    if (widthTranslated > width)
                    {
                        width += widthTranslated - width;
                    }
                }
            }
            else if (!(width > viewport.Width))
            {
                width += Abs(transformed.Position.X);
            }
            
            if (height < viewport.Height)
            {
                height = viewport.Height;
                
                if (transformed.Position.Y < 0.0)
                {
                    height += Abs(transformed.Position.Y);
                }
                else
                {
                    var heightTranslated = transformed.Size.Height + transformed.Position.Y;
                    if (heightTranslated > height)
                    {
                        height += heightTranslated - height;
                    }
                }
            }
            else if (!(height > viewport.Height))
            {
                height += Abs(transformed.Position.Y);
            }

            extent = new Size(width, height);

            var ox = transformed.Position.X;
            var oy = transformed.Position.Y;

            var offsetX = ox < 0 ? Abs(ox) : 0;
            var offsetY = oy < 0 ? Abs(oy) : 0;

            offset = new Vector(offsetX, offsetY);

            ZoomBorder.Log($"[CalculateScrollable] Extent: {extent} | Offset: {offset} | Viewport: {viewport}");
        }
    }
}
