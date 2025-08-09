using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.OpenGL;
using Avalonia.Rendering;
using Avalonia.Threading;
using TheAvaloniaOpenGL;
using TheEngine.Config;
using TheEngine.Utils;
using MouseButton = TheEngine.Input.MouseButton;

namespace TheEngine;

public class NativeTheEnginePanel : Panel, IWindowHost, IDisposable
{
    protected Engine? engine;
    protected GameRunner? gameRunner;
    private Stopwatch sw = new Stopwatch();
    private Stopwatch renderStopwatch = new Stopwatch();
    private Stopwatch updateStopwatch = new();
    private int frame = 0;

    private RollingAverage framerate = new();
    private void Tick(float delta)
    {
        engine.TotalTime += delta;
        framerate.Add(delta);
    }
    
    static NativeTheEnginePanel()
    {
        BackgroundProperty.OverrideDefaultValue<NativeTheEnginePanel>(Brushes.Transparent);
        FocusableProperty.OverrideDefaultValue<NativeTheEnginePanel>(true);
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        engine?.inputManager.keyboard.ReleaseAllKeys();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.P && (e.KeyModifiers & KeyModifiers.Control) != 0)
        {
            innerControl.ToggleProfiling();
        }
        engine?.inputManager.keyboard.KeyDown(e.Key);
        //if (!Undo.Matches(e) && !Redo.Matches(e) && !IsModifierKey(e.Key))
        //    e.Handled = true;
        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        engine?.inputManager.keyboard.KeyUp(e.Key);
        //if (!Undo.Matches(e) && !Redo.Matches(e) && !IsModifierKey(e.Key))
        //    e.Handled = true;
        base.OnKeyUp(e);
    }

    private int lastGotFocusFrame;
    protected override void OnGotFocus(GotFocusEventArgs e)
    {
        lastGotFocusFrame = frame;
        base.OnGotFocus(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        // in the same frame got focus and pressed button, ignore the event
        if (lastGotFocusFrame == frame)
            return;
        var left = e.GetCurrentPoint(this).Properties.IsLeftButtonPressed;
        var right = e.GetCurrentPoint(this).Properties.IsRightButtonPressed;
        engine?.inputManager.mouse.MouseDown((left ? MouseButton.Left : MouseButton.None) | (right ? MouseButton.Right : MouseButton.None));
        base.OnPointerPressed(e);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        engine?.inputManager.mouse.PointerMoved(e.GetPosition(this).X, e.GetPosition(this).Y, this.Bounds.Width, this.Bounds.Height);
        base.OnPointerMoved(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        var left = e.GetCurrentPoint(this).Properties.IsLeftButtonPressed;
        var right = e.GetCurrentPoint(this).Properties.IsRightButtonPressed;
        engine?.inputManager.mouse.MouseUp((left ? MouseButton.Left : MouseButton.None) | (right ? MouseButton.Right : MouseButton.None));
        base.OnPointerReleased(e);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        engine?.inputManager.mouse.MouseWheel(new Vector2((float)e.Delta.X, (float)e.Delta.Y));
        base.OnPointerWheelChanged(e);
    }
    
    private IGame? game;
    private bool gameInitialized;
    public static readonly DirectProperty<NativeTheEnginePanel, IGame?> GameProperty = AvaloniaProperty.RegisterDirect<NativeTheEnginePanel, IGame?>(nameof(Game), o => o.Game, (o, v) => o.Game = v);

    private System.IDisposable? globalKeyDownDisposable;
    private System.IDisposable? globalKeyUpDisposable;
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        sw.Restart();
        globalKeyDownDisposable = ((Control)e.Root).AddDisposableHandler(KeyDownEvent, GlobalKeyDown, RoutingStrategies.Tunnel);
        globalKeyUpDisposable = ((Control)e.Root).AddDisposableHandler(KeyUpEvent, GlobalKeyUp, RoutingStrategies.Tunnel);
    }

    private bool IsModifierKey(Key key) => key is Key.LeftShift or Key.LeftCtrl or Key.LeftAlt or Key.LWin;
    
    private void GlobalKeyDown(object? sender, KeyEventArgs e)
    {
        if (IsModifierKey(e.Key))
            engine?.inputManager.keyboard.KeyDown(e.Key);
    }

    private void GlobalKeyUp(object? sender, KeyEventArgs e)
    {
        if (IsModifierKey(e.Key))
            engine?.inputManager.keyboard.KeyUp(e.Key);
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        if (e.Text != null)
        {
            foreach (var letter in e.Text)
                engine?.inputManager.keyboard.OnTextInput(letter);
            e.Handled = true;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        //if (delayedDispose)
        //    base.OnDetachedFromVisualTree(e);
        //delayedDispose = false;
        globalKeyUpDisposable?.Dispose();
        globalKeyDownDisposable?.Dispose();
        globalKeyUpDisposable = null;
        globalKeyDownDisposable = null;
        sw.Stop();
    }

    protected void InitializeGame()
    {
        if (!gameInitialized && game != null)
        {
            gameInitialized = true;
            game.RequestDispose += GameOnRequestDispose;
            if (!game.Initialize(engine!))
            {
                GameOnRequestDispose();
                game = null;
            }
        }
    }

    private bool disposed;
    private bool delayedDispose;
    private void GameOnRequestDispose()
    {
        if (game != null)
            game.RequestDispose -= GameOnRequestDispose;
        delayedDispose = true;
        disposed = true;
    }
    
    public IGame? Game
    {
        get => game;
        set
        {
            if (game != null)
                game.RequestDispose -= GameOnRequestDispose;
            SetAndRaise(GameProperty, ref game, value);
            gameInitialized = false;
        }
    }

    public void Dispose()
    {
        //DoCleanup();
    }

    public NativeTheEnginePanel()
    {
        Focusable = true;
        innerControl = new InnerControl(this);
        Children.Add(innerControl);
    }

    private InnerControl innerControl;

    private class InnerControl : NativeOpenGlControlBase
    {
        private readonly NativeTheEnginePanel parent;

        public InnerControl(NativeTheEnginePanel parent)
        {
            this.parent = parent;
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            // don't fire ContextMenuRequest here
            // base.OnPointerReleased(e);
        }

        protected override void OnOpenGlInit(GlInterface gl, int fb)
        {
            try
            {
                IDevice device = new OpenTKDevice();
    #if DEBUG && DEBUG_OPENGL
                device = new DebugDevice(device);
    #endif
                parent.engine = new Engine(device, new Configuration(), parent, false);
                parent.gameRunner = new GameRunner(parent.engine);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        protected override void OnOpenGlDeinit(GlInterface gl, int fb)
        {
            parent.game?.DisposeGame();
            parent.engine.Dispose();
            parent.engine = null!;
            base.OnOpenGlDeinit(gl, fb);
        }

        protected override void OnOpenGlRender(GlInterface gl, int fb)
        {
            if (parent.delayedDispose)
            {
                DoCleanup();
                parent.delayedDispose = false;
                return;
            }
            
            if (parent.engine == null || parent.disposed)
                return;

            var engine = parent.engine;
            var gameRunner = parent.gameRunner;
            
            engine.statsManager.PixelSize = new Vector2(PixelSize.Item1, PixelSize.Item2);
            engine.statsManager.Counters.PresentTime.Add(PresentTime);
            try
            {
                engine.Device.device.CheckError("start OnOpenGlRender");
                engine.Device.device.Begin();

                parent.InitializeGame();

                var delta = (float)parent.sw.Elapsed.TotalMilliseconds;
                parent.sw.Restart();
                gameRunner.NextFrame(delta / 1000.0f, parent.game);
                parent.Tick(delta);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                // important! we need to restore Avalonia status
                engine.Device.device.ActiveTextureUnit(0);
                engine.Device.device.CheckError("post render");
            }
            parent.frame++;
        }
    }
    
    public float WindowWidth => (float)Bounds.Width * DpiScaling;
    public float WindowHeight => (float)Bounds.Height * DpiScaling;
    public float DpiScaling => (float?)VisualRoot?.RenderScaling ?? 1.0f;
    public bool HitTest(Point point)
    {
        return true;
    }
}