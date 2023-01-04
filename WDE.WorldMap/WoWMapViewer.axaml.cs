using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using WDE.WorldMap.Models;
using WDE.WorldMap.PanAndZoom;

namespace WDE.WorldMap
{
    public class WoWMapViewer : ContentControl
    {
        private double zoom;
        public static readonly DirectProperty<WoWMapViewer, double> ZoomProperty = AvaloniaProperty.RegisterDirect<WoWMapViewer, double>(nameof(Zoom), o => o.Zoom, (o, v) => o.Zoom = v);
        
        private string? map;
        public static readonly DirectProperty<WoWMapViewer, string?> MapProperty = AvaloniaProperty.RegisterDirect<WoWMapViewer, string?>(nameof(Map), o => o.Map, (o, v) => o.Map = v);
        
        private string? mapsPath;
        public static readonly DirectProperty<WoWMapViewer, string?> MapsPathProperty = AvaloniaProperty.RegisterDirect<WoWMapViewer, string?>(nameof(MapsPath), o => o.MapsPath, (o, v) => o.MapsPath = v);
        
        private IMapContext? mapViewModel;
        public static readonly DirectProperty<WoWMapViewer, IMapContext?> MapViewModelProperty = AvaloniaProperty.RegisterDirect<WoWMapViewer, IMapContext?>(nameof(MapViewModel), o => o.MapViewModel, (o, v) => o.MapViewModel = v);
        
        private bool renderBackground = true;
        public static readonly DirectProperty<WoWMapViewer, bool> RenderBackgroundProperty = AvaloniaProperty.RegisterDirect<WoWMapViewer, bool>(nameof(RenderBackground), o => o.RenderBackground, (o, v) => o.RenderBackground = v);
        
        private Point topLeftVirtual;
        public static readonly DirectProperty<WoWMapViewer, Point> TopLeftVirtualProperty = AvaloniaProperty.RegisterDirect<WoWMapViewer, Point>(nameof(TopLeftVirtual), o => o.TopLeftVirtual, (o, v) => o.TopLeftVirtual = v);
        
        private Point bottomRightVirtual;
        public static readonly DirectProperty<WoWMapViewer, Point> BottomRightVirtualProperty = AvaloniaProperty.RegisterDirect<WoWMapViewer, Point>(nameof(BottomRightVirtual), o => o.BottomRightVirtual, (o, v) => o.BottomRightVirtual = v);

        private ExtendedZoomBorder? zoomBorder;
        
        public double Zoom
        {
            get => zoom;
            set => SetAndRaise(ZoomProperty, ref zoom, value);
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

        private void OnCenter(double x, double y)
        {
            var editorCoords = CoordsUtils.WorldToEditor(x, y);
            if (zoomBorder != null)
                zoomBorder.SetCenter(editorCoords.editorX, editorCoords.editorY);
        }

        private void BoundsToView(double left, double right, double top, double bottom)
        {
            var leftTopEditor = CoordsUtils.WorldToEditor(left - 10, top - 10);
            var rightBottomEditor = CoordsUtils.WorldToEditor(right + 10, bottom + 10);
            if (zoomBorder != null)
                zoomBorder.MoveToBounds(leftTopEditor.editorX, rightBottomEditor.editorX, leftTopEditor.editorY, rightBottomEditor.editorY);
        }

        public IMapContext? MapViewModel
        {
            get => mapViewModel;
            set
            {
                if (mapViewModel != null)
                    Unbind(mapViewModel);
                SetAndRaise(MapViewModelProperty, ref mapViewModel, value);
                if (value != null && Parent != null)
                    Bind(value);
            }
        }

        public bool RenderBackground
        {
            get => renderBackground;
            set => SetAndRaise(RenderBackgroundProperty, ref renderBackground, value);
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

        private void Bind(IMapContext ctx)
        {
            ctx.RequestCenter += OnCenter;
            ctx.RequestBoundsToView += BoundsToView;
            InvalidateVisual();
        }

        private void Unbind(IMapContext ctx)
        {
            ctx.RequestCenter -= OnCenter;
            ctx.RequestBoundsToView -= BoundsToView;
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            if (mapViewModel != null)
            {
                Unbind(mapViewModel);
                Bind(mapViewModel);
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            if (mapViewModel != null)
                Unbind(mapViewModel);
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            zoomBorder = e.NameScope.Find<ExtendedZoomBorder>("ZoomBorder");

            DispatcherTimer.RunOnce(() =>
            {
                if (mapViewModel != null)
                    mapViewModel.Initialized();
            }, TimeSpan.FromMilliseconds(500));
        }
    }
}