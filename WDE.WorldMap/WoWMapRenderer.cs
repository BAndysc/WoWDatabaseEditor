using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace WDE.WorldMap
{
    public class WoWMapRenderer : Control
    {
        private static readonly int[] SizeMultipliers = { 1, 2, 4, 8, 16 };

        static WoWMapRenderer()
        {
            AffectsRender<WoWMapRenderer>(CurrentZoomProperty);

            MapProperty.Changed.AddClassHandler<WoWMapRenderer>((renderer, _) =>
            {
                renderer.loaded = false;
                renderer.InvalidateVisual();
            });

            MapsPathProperty.Changed.AddClassHandler<WoWMapRenderer>((renderer, _) =>
            {
                renderer.loaded = false;
                renderer.InvalidateVisual();
            });
        }

        public double CurrentZoom
        {
            get => currentZoom;
            set => SetAndRaise(CurrentZoomProperty, ref currentZoom, value);
        }

        public Point TopLeftVirtual
        {
            get => topLeftVirtual;
            set => SetAndRaise(TopLeftVirtualProperty, ref topLeftVirtual, value);
        }

        public Point BottomRightVirtual
        {
            get => bottomRightVirtual;
            set => SetAndRaise(BottomRightVirtualProperty, ref bottomRightVirtual, value);
        }

        public string? Map
        {
            get => map;
            set => SetAndRaise(MapProperty, ref map, value);
        }

        public string? MapsPath
        {
            get => mapsPath;
            set => SetAndRaise(MapsPathProperty, ref mapsPath, value);
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            LoadIfNeeded();

            if (tiles[0] == null)
                return;

            int i = 0;
            BitmapInterpolationMode interpo = BitmapInterpolationMode.LowQuality;
            
            if (CurrentZoom < 0.6)
                i = 1;
            if (CurrentZoom < 0.3)
                i = 2;
            if (CurrentZoom < 0.15)
                i = 3;
            if (CurrentZoom < 0.075)
                i = 4;
            
            if (i == 0)
                interpo = BitmapInterpolationMode.HighQuality;
            else if (i == 1)
                interpo = BitmapInterpolationMode.MediumQuality;

            RenderOptions.SetBitmapInterpolationMode(this, interpo);

            foreach (var tile in tiles[i]!)
                context.DrawImage(tile.bitmap, new Rect(tile.bitmap.Size), new Rect(tile.x, tile.y, tile.size, tile.size));
        }

        private bool loaded;

        private List<(double x, double y, double size, Bitmap bitmap)>?[] tiles = new List<(double, double, double, Bitmap)>[5];
        
        private double currentZoom;
        public static readonly DirectProperty<WoWMapRenderer, double> CurrentZoomProperty = AvaloniaProperty.RegisterDirect<WoWMapRenderer, double>("CurrentZoom", o => o.CurrentZoom, (o, v) => o.CurrentZoom = v);
        
        private Point topLeftVirtual;
        public static readonly DirectProperty<WoWMapRenderer, Point> TopLeftVirtualProperty = AvaloniaProperty.RegisterDirect<WoWMapRenderer, Point>("TopLeftVirtual", o => o.TopLeftVirtual, (o, v) => o.TopLeftVirtual = v);
        
        private Point bottomRightVirtual;
        public static readonly DirectProperty<WoWMapRenderer, Point> BottomRightVirtualProperty = AvaloniaProperty.RegisterDirect<WoWMapRenderer, Point>("BottomRightVirtual", o => o.BottomRightVirtual, (o, v) => o.BottomRightVirtual = v);
        
        private string? map;
        public static readonly DirectProperty<WoWMapRenderer, string?> MapProperty = AvaloniaProperty.RegisterDirect<WoWMapRenderer, string?>("Map", o => o.Map, (o, v) => o.Map = v);
        
        private string? mapsPath;
        public static readonly DirectProperty<WoWMapRenderer, string?> MapsPathProperty = AvaloniaProperty.RegisterDirect<WoWMapRenderer, string?>("MapsPath", o => o.MapsPath, (o, v) => o.MapsPath = v);

        private void LoadIfNeeded()
        {
            if (loaded)
                return;
            loaded = true;
            
            foreach (var tileSet in tiles)
            {
                if (tileSet != null)
                {
                    foreach (var t in tileSet)
                        t.bitmap.Dispose();       
                    tileSet.Clear();   
                }
            }

            if (MapsPath == null || Map == null || !Directory.Exists(Path.Combine(MapsPath, $"x1", Map)))
                return;
            
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;

            int i = 0;
            foreach (var chunkSize in SizeMultipliers)
            {
                tiles[i] = new();
                foreach (var file in Directory.GetFiles(Path.Combine(MapsPath, $"x{chunkSize}", Map)))
                {
                    var fileName = Path.GetFileName(file);
                    
                    string posx = fileName.Substring(3, 2);
                    string posy = fileName.Substring(6, 2);
                    if (int.TryParse(posx, out var pposx) && int.TryParse(posy, out var pposy))
                    { 
                        var (worldX, worldY) = CoordsUtils.BlockToWorldCoords(pposx, pposy);
            
                        var (x, y) = CoordsUtils.WorldToEditor(worldX, worldY);
                        var (_, by) = CoordsUtils.WorldToEditor(worldX + CoordsUtils.BlockSize * chunkSize, worldY + CoordsUtils.BlockSize * chunkSize);
                        //float h = (float)Math.Abs(bx - x);
                        float w = (float)Math.Abs(by - y);
                        tiles[i]!.Add((x, y, w, new Bitmap(file)));

                        minX = Math.Min(minX, x);
                        minY = Math.Min(minY, y);
                        maxX = Math.Max(maxX, x);
                        maxY = Math.Max(maxY, y);
                    }
                }

                ++i;
            }

            (minX, minY) = CoordsUtils.WorldToEditor(CoordsUtils.MinCoord, CoordsUtils.MinCoord);
            (maxX, maxY) = CoordsUtils.WorldToEditor(CoordsUtils.MaxCoord, CoordsUtils.MaxCoord);
            TopLeftVirtual = new Point(Math.Min(minX, maxX), Math.Min(minY, maxY));
            BottomRightVirtual = new Point(Math.Max(minX, maxX), Math.Max(minY, maxY));
        }
    }
}