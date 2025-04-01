using System;
using Avalonia.Media;
using Avalonia.Rendering.Composition;

namespace Avalonia.Labs.Gif;

internal class GifCompositionCustomVisualHandler : CompositionCustomVisualHandler
{
    private bool _running;
    private Stretch? _stretch;
    private StretchDirection? _stretchDirection;
    private Size _GifSize;
    private readonly object _sync = new();
    private bool _isDisposed;
    private GifInstance? _gifInstance;

    private TimeSpan _animationElapsed;
    private TimeSpan _lastServerTime;

    public override void OnMessage(object message)
    {
        if (message is not GifDrawPayload msg)
        {
            return;
        }

        switch (msg)
        {
            case
            {
                HandlerCommand: HandlerCommand.Start, Source: { } stream, IterationCount: { } iteration,
                Stretch: { } st, StretchDirection: { } sd
            }:
            {
                _gifInstance = new GifInstance(stream);

                _gifInstance.IterationCount = iteration;

                _lastServerTime = CompositionNow;
                _GifSize = _gifInstance.GifPixelSize.ToSize(1);
                _running = true;
                _stretch = st;
                _stretchDirection = sd;
                RegisterForNextAnimationFrameUpdate();
                break;
            }
            case
            {
                HandlerCommand: HandlerCommand.Update, Stretch: { } st, IterationCount: { } iteration,
                StretchDirection: { } sd
            }:
            {
                _stretch = st;
                _stretchDirection = sd;
                if (_gifInstance != null)
                    _gifInstance.IterationCount = iteration;
                RegisterForNextAnimationFrameUpdate();
                break;
            }
            case { HandlerCommand: HandlerCommand.Stop }:
            {
                _running = false;
                break;
            }
            case { HandlerCommand: HandlerCommand.Dispose }:
            {
                DisposeImpl();
                break;
            }
        }
    }

    public override void OnAnimationFrameUpdate()
    {
        if (!_running || _isDisposed)
            return;

        Invalidate();
        RegisterForNextAnimationFrameUpdate();
    }

    public override void OnRender(ImmediateDrawingContext context)
    {
        lock (_sync)
        {
            if (_stretch is not { } st
                || _stretchDirection is not { } sd
                || _gifInstance is null
                || _isDisposed
                || !_running)
            {
                return;
            }

            _animationElapsed += CompositionNow - _lastServerTime;
            _lastServerTime = CompositionNow;

            var bounds = GetRenderBounds().Size;
            var viewPort = new Rect(bounds);

            var scale = st.CalculateScaling(bounds, _GifSize, sd);
            var scaledSize = _GifSize * scale;
            var destRect = viewPort
                .CenterRect(new Rect(scaledSize))
                .Intersect(viewPort);

            var bitmap = _gifInstance.ProcessFrameTime(_animationElapsed);
            if (bitmap is not null)
            {
                context.DrawBitmap(bitmap, new Rect(_gifInstance.GifPixelSize.ToSize(1)),
                    destRect);
            }
        }
    }

    private void DisposeImpl()
    {
        lock (_sync)
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _gifInstance?.Dispose();
            _animationElapsed = TimeSpan.Zero;
            _lastServerTime = TimeSpan.Zero;
            _running = false;
        }
    }
}
