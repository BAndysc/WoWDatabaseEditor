using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using TheAvaloniaOpenGL;
using TheEngine.Config;
using MouseButton = TheEngine.Input.MouseButton;
using TheEngine.Utils;
using TheMaths;
using Point = Avalonia.Point;

[assembly: InternalsVisibleTo("RenderingTester")]
namespace TheEngine
{
#if USE_OPENTK
    public class TheEnginePanel : OpenTKGlControl2, IWindowHost, IDisposable
#else
    public class TheEnginePanel : OpenGlBase2, IWindowHost, IDisposable, ICustomHitTest
#endif
    {
        protected Engine? engine;
        private Stopwatch sw = new Stopwatch();
        private Stopwatch renderStopwatch = new Stopwatch();
        private Stopwatch updateStopwatch = new();
        private int frame = 0;
        public float FrameRate => 1000.0f / framerate.Average;
        
        public static readonly DirectProperty<TheEnginePanel, float> FrameRateProperty = AvaloniaProperty.RegisterDirect<TheEnginePanel, float>("FrameRate", o => o.FrameRate);

        private RollingAverage framerate = new();
        private void Tick(float delta)
        {
            engine.TotalTime += delta;
            framerate.Add(delta);
        }

        public TheEnginePanel() : base()
        {
            Focusable = true;
            DispatcherTimer.Run(() =>
            {
                var _compo = typeof(OpenGlControlBase).GetField("_compositor", BindingFlags.Instance | BindingFlags.NonPublic);
                if (_compo == null)
                    return true;
                var compositor = _compo.GetValue(this) as Compositor;
                compositor.RequestCompositionUpdate(() =>
                {
                    var updatemethod =
                        typeof(OpenGlControlBase).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
                    updatemethod.Invoke(this, null);
                });
                return true;
            }, TimeSpan.FromMilliseconds(12));
        }

        static TheEnginePanel()
        {
            FocusableProperty.OverrideDefaultValue<TheEnginePanel>(true);
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            engine?.inputManager.keyboard.ReleaseAllKeys();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
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

        protected override void OnOpenGlInit(GlInterface gl)
        {
            try
            {
#if USE_OPENTK
                IDevice device = new OpenTKDevice();
#if DEBUG && DEBUG_OPENGL
                device = new DebugDevice(device);
#endif
                engine = new Engine(device, new Configuration(), this, false);
#else
                IDevice device;
                var real = new RealDevice(gl);
#if DEBUG && DEBUG_OPENGL
                device = new DebugDevice(new RealDeviceWrapper(real));
#else
                device = new RealDeviceWrapper(real);
#endif
                engine = new Engine(device, new Configuration(), this, false);
#endif
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        protected override void OnOpenGlDeinit(GlInterface gl)
        {
            game?.DisposeGame();
            engine.Dispose();
            engine = null!;
            base.OnOpenGlDeinit(gl);
        }

        private PixelSize GetPixelSize(IRenderRoot visualRoot)
        {
            var scaling = visualRoot.RenderScaling;
            return new PixelSize(Math.Max(1, (int)(Bounds.Width * scaling)),
                Math.Max(1, (int)(Bounds.Height * scaling)));
        }
        
        public (float, float) PixelSize
        {
            get
            {
                var pixelSize = GetPixelSize(VisualRoot);
                return (pixelSize.Width, pixelSize.Height);
            }
        }

        protected override void OnOpenGlRender(GlInterface gl, int fb)
        {
            //Dispatcher.UIThread.Post(RequestNextFrameRendering, DispatcherPriority.Render);
            if (delayedDispose)
            {
                Console.WriteLine("Can't do DoCleanup as before, 3D should be fixed");
                //DoCleanup();
                delayedDispose = false;
                return;
            }
            
            if (engine == null || disposed)
                return;
            
            engine.statsManager.PixelSize = new Vector2(PixelSize.Item1, PixelSize.Item2);
            engine.statsManager.Counters.PresentTime.Add(default); // @todo PresentTime
            try
            {
                engine.Device.device.CheckError("start OnOpenGlRender");
                engine.Device.device.Begin();

                var delta = (float)sw.Elapsed.TotalMilliseconds;
                engine.inputManager.Update(delta);
                engine.renderManager.BeginFrame();
                engine.UpdateGui(delta / 1000.0f);
                Tick(delta);
                engine.statsManager.Counters.FrameTime.Add(delta);
                sw.Restart();
                Dispatcher.UIThread.Post(() => RaisePropertyChanged(FrameRateProperty, 0, FrameRate), DispatcherPriority.Render);

                updateStopwatch.Restart();
                Update(delta);
                updateStopwatch.Stop();
                engine.statsManager.Counters.UpdateTime.Add(updateStopwatch.Elapsed.TotalMilliseconds);

                // render pass
                renderStopwatch.Restart();
                engine.renderManager.PrepareRendering(fb);
                engine.renderManager.RenderOpaque(fb);
                game?.Render(delta);
                engine.renderManager.RenderTransparent(fb);
                game?.RenderTransparent(delta);
                engine.renderManager.RenderPostProcess();
                game?.RenderGUI(delta);
                engine.RenderGUI();
                engine.renderManager.FinalizeRendering(fb);
                engine.Device.device.Flush();
                engine.Device.device.Finish();
                renderStopwatch.Stop();
                engine.statsManager.Counters.TotalRender.Add(renderStopwatch.Elapsed.Milliseconds);
                
                if (engine.inputManager.Keyboard.JustPressed(Key.R))
                {
                    if (engine.Device.device is DebugDevice debug)
                    {
                        var file = new FileInfo("render_debug.txt");
                        File.WriteAllLines(file.FullName, debug.commands);
                        Console.WriteLine("Log written to " + file.FullName);
                    }
                }
                engine.inputManager.PostUpdate();
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
            frame++;
        }

        private IGame? game;
        private bool gameInitialized;
        public static readonly DirectProperty<TheEnginePanel, IGame?> GameProperty = AvaloniaProperty.RegisterDirect<TheEnginePanel, IGame?>(nameof(Game), o => o.Game, (o, v) => o.Game = v);

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

        protected virtual void Update(float delta)
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
            game?.Update(delta);
            engine?.renderManager.UpdateTransforms();
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
        
        public float WindowWidth => PixelSize.Item1;
        public float WindowHeight => PixelSize.Item2;
        public float DpiScaling => (float)VisualRoot.RenderScaling;

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
            Console.WriteLine("Can't do DoClanup as before");
            //DoCleanup();
        }

        public bool HitTest(Point point)
        {
            return true;
        }
    }
}
