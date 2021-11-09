using System;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.OpenTK;
using Avalonia.Threading;
using OpenGLBindings;
using TheAvaloniaOpenGL;
using TheEngine.Config;
using MouseButton = TheEngine.Input.MouseButton;
using OpenTK.Graphics.OpenGL;

namespace TheEngine
{
#if USE_OPENTK
    public class TheEnginePanel : OpenTKGlControl, IWindowHost, IDisposable
#else
    public class TheEnginePanel : OpenGlBase, IWindowHost, IDisposable
#endif
    {
        protected Engine? engine;
        public TheDevice device;
        private Stopwatch sw = new Stopwatch();

        public float FrameRate => 1000.0f / lastDeltas.Average();
        
        public static readonly DirectProperty<TheEnginePanel, float> FrameRateProperty = AvaloniaProperty.RegisterDirect<TheEnginePanel, float>("FrameRate", o => o.FrameRate);

        private float[] lastDeltas = new float[20];
        private int lastDeltaIndex;
        private void Tick(float delta)
        {
            lastDeltas[lastDeltaIndex++] = delta;
            lastDeltaIndex %= lastDeltas.Length;
        }
        
#if USE_OPENTK
        public TheEnginePanel() : base(new OpenGlControlSettings
        {
            ContinuouslyRender = true,
            DeInitializeOnVisualTreeDetachment = false,
        }) { }
#endif

        static TheEnginePanel()
        {
            FocusableProperty.OverrideDefaultValue<TheEnginePanel>(true);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            engine?.inputManager.keyboard.KeyDown(e.Key);
            e.Handled = true;
            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            engine?.inputManager.keyboard.KeyUp(e.Key);
            e.Handled = true;
            base.OnKeyUp(e);
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
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
                engine = new Engine(new DebugDevice(new RealDevice(gl)), new Configuration(), this);
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
            game?.Dispose();
            engine.Dispose();
            engine = null!;
            base.OnOpenGlDeinit(gl, fb);
        }

        protected override void OnOpenGlRender(GlInterface gl, int fb)
        {
            try
            {
                engine.Device.device.CheckError("start OnOpenGlRender");
                engine.Device.device.Begin();
                engine.inputManager.Update();
                engine.renderManager.BeginFrame();

                var delta = (float)sw.Elapsed.TotalMilliseconds;
                Tick(delta);
                sw.Restart();
                RaisePropertyChanged(FrameRateProperty, Optional<float>.Empty, FrameRate);

                Update(delta);
                engine.inputManager.PostUpdate();

                // render pass
                engine.renderManager.PrepareRendering(fb);
                engine.renderManager.RenderWorld(fb);
                Render(delta);
                engine.renderManager.FinalizeRendering(fb);
                Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Render);
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
        }

        private IGame? game;
        private bool gameInitialized;
        public static readonly DirectProperty<TheEnginePanel, IGame?> GameProperty = AvaloniaProperty.RegisterDirect<TheEnginePanel, IGame?>(nameof(Game), o => o.Game, (o, v) => o.Game = v);

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            sw.Restart();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            //base.OnDetachedFromVisualTree(e);
            sw.Stop();
        }

        protected virtual void Update(float delta)
        {
            if (!gameInitialized && game != null)
            {
                gameInitialized = true;
                game.Initialize(engine!);
            }
            game?.Update(delta);
        }

        protected virtual void Render(float delta)
        {
            game?.Render(delta);
        }
        
        public float WindowWidth => (float)Bounds.Width;
        public float WindowHeight => (float)Bounds.Height;

        public IGame? Game
        {
            get => game;
            set
            {
                SetAndRaise(GameProperty, ref game, value);
                gameInitialized = false;
            }
        }

        public void Dispose()
        {
            //DoCleanup();
        }
    }
}
