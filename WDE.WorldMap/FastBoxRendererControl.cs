using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using WDE.WorldMap.Extensions;
using WDE.WorldMap.Models;

namespace WDE.WorldMap
{
    public abstract class FastBoxRendererControl<T, R> : Control where T : class, IMapItem where R : class, IMapContext<T>
    {
        private bool dragging;
        private bool draggingItem;
        private Point startDrag;
        private Point startPos;

        #pragma warning disable AVP1002
        private double zoomBias;
        public static readonly DirectProperty<FastBoxRendererControl<T, R>, double> ZoomBiasProperty = AvaloniaProperty.RegisterDirect<FastBoxRendererControl<T, R>, double>("ZoomBias", o => o.ZoomBias, (o, v) => o.ZoomBias = v);

        private R? context;
        public static readonly DirectProperty<FastBoxRendererControl<T, R>, R?> ContextProperty = AvaloniaProperty.RegisterDirect<FastBoxRendererControl<T, R>, R?>("Context", o => o.Context, (o, v) => o.Context = v);

        private bool renderMarkers = true;
        public static readonly DirectProperty<FastBoxRendererControl<T, R>, bool> RenderMarkersProperty = AvaloniaProperty.RegisterDirect<FastBoxRendererControl<T, R>, bool>("RenderMarkers", o => o.RenderMarkers, (o, v) => o.RenderMarkers = v);
        #pragma warning restore AVP1002

        private bool isAttachedToVisualTree;
        
        static FastBoxRendererControl()
        {
            AffectsRender<FastBoxRendererControl<T, R>>(ZoomBiasProperty, RenderMarkersProperty);
        }

        public R? Context
        {
            get => context;
            set
            {
                if (context != null)
                    Unbind(context);
                SetAndRaise(ContextProperty, ref context, value);
                if (value != null && Parent != null && isAttachedToVisualTree)
                    Bind(value);
            }
        }

        public bool RenderMarkers
        {
            get => renderMarkers;
            set => SetAndRaise(RenderMarkersProperty, ref renderMarkers, value);
        }

        public double ZoomBias
        {
            get => zoomBias;
            set => SetAndRaise(ZoomBiasProperty, ref zoomBias, value);
        }

        private void Bind(R ctx)
        {
            ctx.RequestRender += ContextOnRequestRender;
            InvalidateVisual();
        }

        private void Unbind(R ctx)
        {
            ctx.RequestRender -= ContextOnRequestRender;
        }

        private System.IDisposable? attached;
        
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            attached?.Dispose();
            attached = null;
            if (context != null)
            {
                Unbind(context);
                Bind(context);

                var parent = e.Parent;

                while (parent != null && parent is not WoWMapViewer)
                    parent = parent.GetVisualParent();

                if (parent is WoWMapViewer map)
                {
                    attached = Bind(ZoomBiasProperty, map.GetBindingObservable(WoWMapViewer.ZoomProperty));
                }
            }

            isAttachedToVisualTree = true;
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            isAttachedToVisualTree = false;
            attached?.Dispose();
            attached = null;
            if (context != null)
                Unbind(context);
        }

        private void ContextOnRequestRender()
        {
            Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Render);
        }

        protected abstract Rect DrawItem(DrawingContext ctx, T item);

        protected virtual void BeforeRender(DrawingContext ctx) {}
        protected virtual void AfterRender(DrawingContext ctx) {}
        
        public override void Render(DrawingContext context)
        {
            base.Render(context);

            if (Context == null)
                return;
            
            context.FillRectangle(Brushes.Transparent, new Rect(CoordsUtils.MinCoord, CoordsUtils.MinCoord, CoordsUtils.TotalSize, CoordsUtils.TotalSize));

            BeforeRender(context);

            if (renderMarkers)
            {
                T? selected = null;
                foreach (var item in Context.VisibleItems)
                {
                    item.VirtualBounds = DrawItem(context, item);
                    if (item == Context.SelectedItem)
                        selected = item;
                }
            
                if (selected != null)
                    DrawItem(context, selected);   
            }

            AfterRender(context);
        }
        
        protected void StartDrag(T? item, Point startPoint)
        {
            startDrag = startPoint;
            if (item == null)
            {
                dragging = true;
                draggingItem = false;
            }
            else
            {
                dragging = false;
                draggingItem = true;
                startPos = new Point(item.X, item.Y);
                Context?.StartMove();
            }
        }
        
        protected void StopDrag(Point? endPoint)
        {
            if (dragging)
            {
                dragging = false;
            }

            if (draggingItem)
            {
                draggingItem = false;
                
                if (endPoint != null && startDrag.Distance(endPoint.Value) < 0.001f)
                    return;
            
                Context?.StopMove();
            }
        }
        
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);

            if (Context == null)
                return;

            if (draggingItem && Context.SelectedItem != null)
            {
                var pos = e.GetPosition(this);
                var diff = pos - startDrag;
                var worldDiff = CoordsUtils.EditorToWorld(diff.X, diff.Y);
                Context.Move(Context.SelectedItem, startPos.X + worldDiff.x, startPos.Y + worldDiff.y);
            }
        }

        private Point mouseStartPressPosition;
        private bool willTryUncheck;
        
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            if (Context == null)
                return;

            if (e.Handled)
                return;

            mouseStartPressPosition = e.GetPosition(this);
            
            var mouse = e.GetPosition(this);
            var under = PickUnder(mouse);
            if (under != null)
                Context.SelectedItem = under;
            else
            {
                willTryUncheck = true;
            }
            
            if (Context.SelectedItem != null && under == Context.SelectedItem)
            {
                e.Handled = true;
                StartDrag(Context.SelectedItem, mouse);
            }
            
            InvalidateVisual();
        }
        
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            
            if (Context == null)
                return;

            if (e.Handled)
                return;
            
            if (willTryUncheck)
            {
                var mouse = e.GetPosition(this);
                if (mouse.Distance(mouseStartPressPosition) > 5)
                {
                    var under = PickUnder(mouse);
                    if (under == null)
                    {
                        Context.SelectedItem = null;
                        e.Handled = true;
                    }
                }
                
                willTryUncheck = false;
            }
            StopDrag(e.GetPosition(this));
        }

        protected override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);
            StopDrag(null);
        }

        private T? PickUnder(Point mouse)
        {
            if (Context == null || !renderMarkers)
                return null;

            foreach (var p in Context.VisibleItems)
            {
                if (p.VirtualBounds.Contains(mouse))
                    return p;
            }

            return null;
        }
    }
}