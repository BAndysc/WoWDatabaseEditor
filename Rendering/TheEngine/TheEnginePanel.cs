using System;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;
using TheAvaloniaOpenGL;
using TheEngine.Config;
using MouseButton = TheEngine.Input.MouseButton;
using TheEngine.Utils;
using TheMaths;

namespace TheEngine
{
#if USE_OPENTK
    public class TheEnginePanel : OpenTKGlControl2, IWindowHost, IDisposable
#else
    public class TheEnginePanel : OpenGlBase2, IWindowHost, IDisposable
#endif
    {
        public static KeyGesture Undo { get; } = AvaloniaLocator.Current
            .GetService<PlatformHotkeyConfiguration>()?.Undo.FirstOrDefault() ?? new KeyGesture(Key.Z, KeyModifiers.Control);

        public static KeyGesture Redo { get; } = AvaloniaLocator.Current
            .GetService<PlatformHotkeyConfiguration>()?.Redo.FirstOrDefault() ?? new KeyGesture(Key.Y, KeyModifiers.Control);
        
        protected Engine? engine;
        private Stopwatch sw = new Stopwatch();
        private Stopwatch renderStopwatch = new Stopwatch();
        private int frame = 0;
        public float FrameRate => 1000.0f / framerate.Average;
        
        public static readonly DirectProperty<TheEnginePanel, float> FrameRateProperty = AvaloniaProperty.RegisterDirect<TheEnginePanel, float>("FrameRate", o => o.FrameRate);

        private RollingAverage framerate = new();
        private void Tick(float delta)
        {
            engine.TotalTime += delta;
            framerate.Add(delta);
        }
        
        public TheEnginePanel() : base(new OpenGlControlSettings
        {
            ContinuouslyRender = true,
            //DeInitializeOnVisualTreeDetachment = false,
        }) { }

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
            if (!Undo.Matches(e) && !Redo.Matches(e) && !IsModifierKey(e.Key))
                e.Handled = true;
            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            engine?.inputManager.keyboard.KeyUp(e.Key);
            if (!Undo.Matches(e) && !Redo.Matches(e) && !IsModifierKey(e.Key))
                e.Handled = true;
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
            engine?.inputManager.mouse.MouseWheel(e.Delta.Length > 0 ? (short)1 : (short)-1);
            base.OnPointerWheelChanged(e);
        }

        protected override void OnOpenGlInit(GlInterface gl, int fb)
        {
            try
            {
#if USE_OPENTK
            engine = new Engine(new OpenTKDevice(), new Configuration(), this);
#else
                IDevice device;
                var real = new RealDevice(gl);
#if DEBUG && DEBUG_OPENGL
                device = new DebugDevice(real);
#else
                device = new RealDeviceWrapper(real);
#endif
                engine = new Engine(device, new Configuration(), this, true);
#endif
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        protected override void OnOpenGlDeinit(GlInterface gl, int fb)
        {
            game?.DisposeGame();
            engine.Dispose();
            engine = null!;
            base.OnOpenGlDeinit(gl, fb);
        }

        protected override void OnOpenGlRender(GlInterface gl, int fb)
        {
            if (engine == null || delayedDispose)
                return;
            
            engine.statsManager.PixelSize = new Vector2(PixelSize.Item1, PixelSize.Item2);
            engine.statsManager.Counters.PresentTime.Add(PresentTime);
            renderStopwatch.Restart();
            try
            {
                engine.Device.device.CheckError("start OnOpenGlRender");
                engine.Device.device.Begin();
                engine.inputManager.Update();
                engine.renderManager.BeginFrame();

                var delta = (float)sw.Elapsed.TotalMilliseconds;
                Tick(delta);
                engine.statsManager.Counters.FrameTime.Add(delta);
                sw.Restart();
                Dispatcher.UIThread.Post(() => RaisePropertyChanged(FrameRateProperty, Optional<float>.Empty, FrameRate), DispatcherPriority.Render);

                Update(delta);

                // render pass
                engine.renderManager.PrepareRendering(fb);
                engine.renderManager.RenderWorld(fb);
                Render(delta);
                engine.renderManager.FinalizeRendering(fb);
                
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
            renderStopwatch.Stop();
            engine.statsManager.Counters.TotalRender.Add(renderStopwatch.Elapsed.Milliseconds);
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
            globalKeyDownDisposable = ((IControl)e.Root).AddDisposableHandler(KeyDownEvent, GlobalKeyDown, RoutingStrategies.Tunnel);
            globalKeyUpDisposable = ((IControl)e.Root).AddDisposableHandler(KeyUpEvent, GlobalKeyUp, RoutingStrategies.Tunnel);
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
        }

        private bool delayedDispose;
        private void GameOnRequestDispose()
        {
            if (game != null)
                game.RequestDispose -= GameOnRequestDispose;
            delayedDispose = true;
            Cleanup();
        }

        protected virtual void Render(float delta)
        {
            game?.Render(delta);
            game?.RenderGUI(delta);
        }

        public float WindowWidth => PixelSize.Item1;
        public float WindowHeight => PixelSize.Item2;

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
            Cleanup();
        }
    }
}
