using System.Diagnostics;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;
using TheEngine;
using TheEngineAvalonia;
using TheEngineAvalonia.Interfaces;
using TheEngine.Config;
using TheEngine.Resources;
using TheEngine.Utils;
using TheEngine.Vulkan;
using Veldrid;
using MouseButton = TheEngine.Input.MouseButton;
using Point = Avalonia.Point;

namespace TheEngine;

public class NativeTheEnginePanel : Panel, IWindowHost, IDisposable
{
    protected Engine? engine;
    protected GameRunner? gameRunner;
    private Stopwatch sw = new Stopwatch();
    private int frame = 0;

    private readonly InnerControl innerControl;

    public NativeTheEnginePanel()
    {
        Focusable = true;
        Children.Add(innerControl = new InnerControl(this));
    }

    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new NoneAutomationPeer(new Control() /* I think there is a bug and NoneAutomationPeer holds a reference to the owner in macOS... */);
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        // only force-release when the pointer left the view too: focus blinks away on world clicks
        // (hosted documents activating) and releasing held WASD there kills the camera mid-flight;
        // while the pointer stays over the view, the window-wide key-up handler covers real releases
        if (!IsPointerOver)
            engine?.inputManager.keyboard.ReleaseAllKeys();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        PushEvent(new InputEvent()
        {
            type = InputEvent.Type.Down,
            key = ConvertKey(e.Key)
        });
        //if (!Undo.Matches(e) && !Redo.Matches(e) && !IsModifierKey(e.Key))
        //    e.Handled = true;
        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        PushEvent(new InputEvent()
        {
            type = InputEvent.Type.Up,
            key = ConvertKey(e.Key)
        });
        //if (!Undo.Matches(e) && !Redo.Matches(e) && !IsModifierKey(e.Key))
        //    e.Handled = true;
        base.OnKeyUp(e);
    }

    private int lastGotFocusFrame;
    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        lastGotFocusFrame = frame;
        base.OnGotFocus(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        // in the same frame got focus and pressed button, ignore the event
        if (lastGotFocusFrame == frame)
            return;
        var props = e.GetCurrentPoint(this).Properties;
        PushEvent(new InputEvent()
        {
            type = InputEvent.Type.MouseDown,
            button = (props.IsLeftButtonPressed ? MouseButton.Left : MouseButton.None) |
                     (props.IsRightButtonPressed ? MouseButton.Right : MouseButton.None) |
                     (props.IsMiddleButtonPressed ? MouseButton.Middle : MouseButton.None)
        });
        base.OnPointerPressed(e);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        PushEvent(new InputEvent()
        {
            type = InputEvent.Type.MouseMove,
            mouseMoveOrWheel = new Vector4((float)e.GetPosition(this).X, (float)e.GetPosition(this).Y, (float)Bounds.Width, (float)Bounds.Height)
        });
        base.OnPointerMoved(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        var props = e.GetCurrentPoint(this).Properties;
        PushEvent(new InputEvent()
        {
            type = InputEvent.Type.MouseUp,
            button = (props.IsLeftButtonPressed ? MouseButton.Left : MouseButton.None) |
                     (props.IsRightButtonPressed ? MouseButton.Right : MouseButton.None) |
                     (props.IsMiddleButtonPressed ? MouseButton.Middle : MouseButton.None)
        });
        base.OnPointerReleased(e);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        PushEvent(new InputEvent()
        {
            type = InputEvent.Type.MouseWheel,
            mouseMoveOrWheel = new Vector4((float)e.Delta.X, (float)e.Delta.Y, 0, 0)
        });
        base.OnPointerWheelChanged(e);
    }
    
    private IGame? game;
    public static readonly DirectProperty<NativeTheEnginePanel, IGame?> GameProperty = AvaloniaProperty.RegisterDirect<NativeTheEnginePanel, IGame?>(nameof(Game), o => o.Game, (o, v) => o.Game = v);

    private IDisposable? globalKeyDownDisposable;
    private IDisposable? globalKeyUpDisposable;
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
        {
            PushEvent(new InputEvent()
            {
                type = InputEvent.Type.Down,
                key = ConvertKey(e.Key)
            });
        }
    }

    private Input.Key ConvertKey(Key key)
    {
        if ((int) key >= 0 && (int) key < AvaloniaKeyToKey.Length)
            return AvaloniaKeyToKey[(int) key];
        return Input.Key.None;
    }

    // ALL key-ups are captured window-wide (not just modifiers): a key released while some other
    // control has focus must still clear the engine's key state, or it stays stuck down
    private void GlobalKeyUp(object? sender, KeyEventArgs e)
    {
        PushEvent(new InputEvent()
        {
            type = InputEvent.Type.Up,
            key = ConvertKey(e.Key)
        });
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        if (e.Text != null)
        {
            foreach (var letter in e.Text)
                PushEvent(new InputEvent()
                {
                    type = InputEvent.Type.Letter,
                    enterLetter = letter
                });
            e.Handled = true;
        }
    }

    private struct InputEvent
    {
        public enum Type
        {
            Letter,
            Up,
            Down,
            MouseDown,
            MouseUp,
            MouseWheel,
            MouseMove
        }

        public Type type;
        public char enterLetter;
        public global::TheEngine.Input.Key key;
        public MouseButton button;
        public Vector4 mouseMoveOrWheel;
    }

    private void PushEvent(InputEvent e)
    {
        keyEvents.Add(e);
    }

    private DoubleBufferedList<InputEvent> keyEvents = new();

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

    private void GameOnRequestDispose()
    {
        if (game != null)
            game.RequestDispose -= GameOnRequestDispose;
        // the base control's render loop (dedicated thread or UI timer, both keep running while
        // the control is detached) consumes the request and calls OnVulkanDeinit on the rendering
        // thread, then stops itself and frees the native window - never torn down inline here,
        // that would race a frame in flight on the render thread
        innerControl.RequestCleanup();
    }
    
    public IGame? Game
    {
        get => game;
        set
        {
            if (game != null)
                game.RequestDispose -= GameOnRequestDispose;
            SetAndRaise(GameProperty, ref game, value);
            if (value != null)
                game.RequestDispose += GameOnRequestDispose;
        }
    }

    public void Dispose()
    {
        // teardown is driven by IGame.RequestDispose (GameOnRequestDispose), not by this
    }

    private class InnerControl : NativeVulkanControlBase
    {
        private readonly NativeTheEnginePanel parent;

        public InnerControl(NativeTheEnginePanel parent)
        {
            this.parent = parent;
        }

        protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
            return base.CreateNativeControlCore(parent);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            // don't fire ContextMenuRequest here
            // base.OnPointerReleased(e);
        }

        protected override void OnVulkanInit(IRenderingWindow window)
        {
            try
            {
                var ctx = new VulkanContext();
                ctx.CreateInstance(window.RequiredInstanceExtensions);

                var surface = window.CreateSurface(ctx.vk, ctx.Instance);

                ctx.PickDeviceAndCreate(surface);
                Console.WriteLine($"Vulkan device: {ctx.DeviceName}");

                var backend = new VulkanRenderBackend(ctx, surface, parent);
                parent.engine = new Engine(backend, new Configuration(), parent, false);
                parent.gameRunner = new GameRunner(parent.engine);
                parent.gameRunner.SyncInputState += () =>
                {
                    var inputs = parent.keyEvents.Collect();
                    foreach (var e in inputs)
                    {
                        switch (e.type)
                        {
                            case InputEvent.Type.Letter:
                                parent.engine.inputManager.keyboard.OnTextInput(e.enterLetter);
                                break;
                            case InputEvent.Type.Up:
                                parent.engine.inputManager.keyboard.KeyUp(e.key);
                                break;
                            case InputEvent.Type.Down:
                                parent.engine.inputManager.keyboard.KeyDown(e.key);
                                break;
                            case InputEvent.Type.MouseMove:
                                parent.engine.inputManager.mouse.PointerMoved(e.mouseMoveOrWheel.X, e.mouseMoveOrWheel.Y, e.mouseMoveOrWheel.Z, e.mouseMoveOrWheel.W);
                                break;
                            case InputEvent.Type.MouseUp:
                                parent.engine.inputManager.mouse.MouseUp(e.button);
                                break;
                            case InputEvent.Type.MouseDown:
                                parent.engine.inputManager.mouse.MouseDown(e.button);
                                break;
                            case InputEvent.Type.MouseWheel:
                                parent.engine.inputManager.mouse.MouseWheel(new Vector2(e.mouseMoveOrWheel.X, e.mouseMoveOrWheel.Y));
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    inputs.Clear();
                };
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        protected override void OnVulkanDeinit()
        {
            if (parent.engine == null)
                return;
            parent.game?.DisposeGame();
            // flush continuations posted to the game loop during the dispose sequence -
            // NextFrame won't run again, so anything left queued would hang its awaiters
            parent.gameRunner?.DrainPendingWork();
            parent.engine.Dispose();
            parent.engine = null!;
            base.OnVulkanDeinit();
            var danglingReferences = Static.values;
        }

        protected override void OnVulkanRender()
        {
            if (parent.engine == null)
                return;

            var engine = parent.engine;
            var gameRunner = parent.gameRunner;

            engine.statsManager.PixelSize = new Vector2(PixelSize.Width, PixelSize.Height);
            engine.statsManager.Counters.PresentTime.Add(PresentTime);
            try
            {
                var delta = (float)parent.sw.Elapsed.TotalMilliseconds;

                parent.sw.Restart();
                if (!gameRunner.NextFrame(delta / 1000.0f, parent.game))
                {
                    // the game failed to initialize - tear down the same way a dispose request would
                    RequestCleanup();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            parent.frame++;
        }
    }
    
    public float WindowWidth => (float)Bounds.Width * DpiScaling;
    public float WindowHeight => (float)Bounds.Height * DpiScaling;
    public float DpiScaling
    {
        get
        {
            if (OperatingSystem.IsMacOS())
                return 1;
            return (float?)TopLevel.GetTopLevel(VisualRoot)?.RenderScaling ?? 1.0f;
        }
    }

    public bool HitTest(Point point)
    {
        return true;
    }
    
    private static global::TheEngine.Input.Key[] AvaloniaKeyToKey = new global::TheEngine.Input.Key[(int)Key.DeadCharProcessed + 1];

    static NativeTheEnginePanel()
    {
        BackgroundProperty.OverrideDefaultValue<NativeTheEnginePanel>(Brushes.Transparent);
        FocusableProperty.OverrideDefaultValue<NativeTheEnginePanel>(true);
        
        AvaloniaKeyToKey[(int)Key.None] = Input.Key.None;
        AvaloniaKeyToKey[(int)Key.Cancel] = Input.Key.Cancel;
        AvaloniaKeyToKey[(int)Key.Back] = Input.Key.Back;
        AvaloniaKeyToKey[(int)Key.Tab] = Input.Key.Tab;
        AvaloniaKeyToKey[(int)Key.LineFeed] = Input.Key.LineFeed;
        AvaloniaKeyToKey[(int)Key.Clear] = Input.Key.Clear;
        AvaloniaKeyToKey[(int)Key.Return] = Input.Key.Return;
        AvaloniaKeyToKey[(int)Key.Enter] = Input.Key.Enter;
        AvaloniaKeyToKey[(int)Key.Pause] = Input.Key.Pause;
        AvaloniaKeyToKey[(int)Key.CapsLock] = Input.Key.CapsLock;
        AvaloniaKeyToKey[(int)Key.Capital] = Input.Key.Capital;
        AvaloniaKeyToKey[(int)Key.HangulMode] = Input.Key.HangulMode;
        AvaloniaKeyToKey[(int)Key.KanaMode] = Input.Key.KanaMode;
        AvaloniaKeyToKey[(int)Key.JunjaMode] = Input.Key.JunjaMode;
        AvaloniaKeyToKey[(int)Key.FinalMode] = Input.Key.FinalMode;
        AvaloniaKeyToKey[(int)Key.KanjiMode] = Input.Key.KanjiMode;
        AvaloniaKeyToKey[(int)Key.HanjaMode] = Input.Key.HanjaMode;
        AvaloniaKeyToKey[(int)Key.Escape] = Input.Key.Escape;
        AvaloniaKeyToKey[(int)Key.ImeConvert] = Input.Key.ImeConvert;
        AvaloniaKeyToKey[(int)Key.ImeNonConvert] = Input.Key.ImeNonConvert;
        AvaloniaKeyToKey[(int)Key.ImeAccept] = Input.Key.ImeAccept;
        AvaloniaKeyToKey[(int)Key.ImeModeChange] = Input.Key.ImeModeChange;
        AvaloniaKeyToKey[(int)Key.Space] = Input.Key.Space;
        AvaloniaKeyToKey[(int)Key.PageUp] = Input.Key.PageUp;
        AvaloniaKeyToKey[(int)Key.Prior] = Input.Key.Prior;
        AvaloniaKeyToKey[(int)Key.PageDown] = Input.Key.PageDown;
        AvaloniaKeyToKey[(int)Key.Next] = Input.Key.Next;
        AvaloniaKeyToKey[(int)Key.End] = Input.Key.End;
        AvaloniaKeyToKey[(int)Key.Home] = Input.Key.Home;
        AvaloniaKeyToKey[(int)Key.Left] = Input.Key.Left;
        AvaloniaKeyToKey[(int)Key.Up] = Input.Key.Up;
        AvaloniaKeyToKey[(int)Key.Right] = Input.Key.Right;
        AvaloniaKeyToKey[(int)Key.Down] = Input.Key.Down;
        AvaloniaKeyToKey[(int)Key.Select] = Input.Key.Select;
        AvaloniaKeyToKey[(int)Key.Print] = Input.Key.Print;
        AvaloniaKeyToKey[(int)Key.Execute] = Input.Key.Execute;
        AvaloniaKeyToKey[(int)Key.Snapshot] = Input.Key.Snapshot;
        AvaloniaKeyToKey[(int)Key.PrintScreen] = Input.Key.PrintScreen;
        AvaloniaKeyToKey[(int)Key.Insert] = Input.Key.Insert;
        AvaloniaKeyToKey[(int)Key.Delete] = Input.Key.Delete;
        AvaloniaKeyToKey[(int)Key.Help] = Input.Key.Help;
        AvaloniaKeyToKey[(int)Key.D0] = Input.Key.D0;
        AvaloniaKeyToKey[(int)Key.D1] = Input.Key.D1;
        AvaloniaKeyToKey[(int)Key.D2] = Input.Key.D2;
        AvaloniaKeyToKey[(int)Key.D3] = Input.Key.D3;
        AvaloniaKeyToKey[(int)Key.D4] = Input.Key.D4;
        AvaloniaKeyToKey[(int)Key.D5] = Input.Key.D5;
        AvaloniaKeyToKey[(int)Key.D6] = Input.Key.D6;
        AvaloniaKeyToKey[(int)Key.D7] = Input.Key.D7;
        AvaloniaKeyToKey[(int)Key.D8] = Input.Key.D8;
        AvaloniaKeyToKey[(int)Key.D9] = Input.Key.D9;
        AvaloniaKeyToKey[(int)Key.A] = Input.Key.A;
        AvaloniaKeyToKey[(int)Key.B] = Input.Key.B;
        AvaloniaKeyToKey[(int)Key.C] = Input.Key.C;
        AvaloniaKeyToKey[(int)Key.D] = Input.Key.D;
        AvaloniaKeyToKey[(int)Key.E] = Input.Key.E;
        AvaloniaKeyToKey[(int)Key.F] = Input.Key.F;
        AvaloniaKeyToKey[(int)Key.G] = Input.Key.G;
        AvaloniaKeyToKey[(int)Key.H] = Input.Key.H;
        AvaloniaKeyToKey[(int)Key.I] = Input.Key.I;
        AvaloniaKeyToKey[(int)Key.J] = Input.Key.J;
        AvaloniaKeyToKey[(int)Key.K] = Input.Key.K;
        AvaloniaKeyToKey[(int)Key.L] = Input.Key.L;
        AvaloniaKeyToKey[(int)Key.M] = Input.Key.M;
        AvaloniaKeyToKey[(int)Key.N] = Input.Key.N;
        AvaloniaKeyToKey[(int)Key.O] = Input.Key.O;
        AvaloniaKeyToKey[(int)Key.P] = Input.Key.P;
        AvaloniaKeyToKey[(int)Key.Q] = Input.Key.Q;
        AvaloniaKeyToKey[(int)Key.R] = Input.Key.R;
        AvaloniaKeyToKey[(int)Key.S] = Input.Key.S;
        AvaloniaKeyToKey[(int)Key.T] = Input.Key.T;
        AvaloniaKeyToKey[(int)Key.U] = Input.Key.U;
        AvaloniaKeyToKey[(int)Key.V] = Input.Key.V;
        AvaloniaKeyToKey[(int)Key.W] = Input.Key.W;
        AvaloniaKeyToKey[(int)Key.X] = Input.Key.X;
        AvaloniaKeyToKey[(int)Key.Y] = Input.Key.Y;
        AvaloniaKeyToKey[(int)Key.Z] = Input.Key.Z;
        AvaloniaKeyToKey[(int)Key.LWin] = Input.Key.LWin;
        AvaloniaKeyToKey[(int)Key.RWin] = Input.Key.RWin;
        AvaloniaKeyToKey[(int)Key.Apps] = Input.Key.Apps;
        AvaloniaKeyToKey[(int)Key.Sleep] = Input.Key.Sleep;
        AvaloniaKeyToKey[(int)Key.NumPad0] = Input.Key.NumPad0;
        AvaloniaKeyToKey[(int)Key.NumPad1] = Input.Key.NumPad1;
        AvaloniaKeyToKey[(int)Key.NumPad2] = Input.Key.NumPad2;
        AvaloniaKeyToKey[(int)Key.NumPad3] = Input.Key.NumPad3;
        AvaloniaKeyToKey[(int)Key.NumPad4] = Input.Key.NumPad4;
        AvaloniaKeyToKey[(int)Key.NumPad5] = Input.Key.NumPad5;
        AvaloniaKeyToKey[(int)Key.NumPad6] = Input.Key.NumPad6;
        AvaloniaKeyToKey[(int)Key.NumPad7] = Input.Key.NumPad7;
        AvaloniaKeyToKey[(int)Key.NumPad8] = Input.Key.NumPad8;
        AvaloniaKeyToKey[(int)Key.NumPad9] = Input.Key.NumPad9;
        AvaloniaKeyToKey[(int)Key.Multiply] = Input.Key.Multiply;
        AvaloniaKeyToKey[(int)Key.Add] = Input.Key.Add;
        AvaloniaKeyToKey[(int)Key.Separator] = Input.Key.Separator;
        AvaloniaKeyToKey[(int)Key.Subtract] = Input.Key.Subtract;
        AvaloniaKeyToKey[(int)Key.Decimal] = Input.Key.Decimal;
        AvaloniaKeyToKey[(int)Key.Divide] = Input.Key.Divide;
        AvaloniaKeyToKey[(int)Key.F1] = Input.Key.F1;
        AvaloniaKeyToKey[(int)Key.F2] = Input.Key.F2;
        AvaloniaKeyToKey[(int)Key.F3] = Input.Key.F3;
        AvaloniaKeyToKey[(int)Key.F4] = Input.Key.F4;
        AvaloniaKeyToKey[(int)Key.F5] = Input.Key.F5;
        AvaloniaKeyToKey[(int)Key.F6] = Input.Key.F6;
        AvaloniaKeyToKey[(int)Key.F7] = Input.Key.F7;
        AvaloniaKeyToKey[(int)Key.F8] = Input.Key.F8;
        AvaloniaKeyToKey[(int)Key.F9] = Input.Key.F9;
        AvaloniaKeyToKey[(int)Key.F10] = Input.Key.F10;
        AvaloniaKeyToKey[(int)Key.F11] = Input.Key.F11;
        AvaloniaKeyToKey[(int)Key.F12] = Input.Key.F12;
        AvaloniaKeyToKey[(int)Key.F13] = Input.Key.F13;
        AvaloniaKeyToKey[(int)Key.F14] = Input.Key.F14;
        AvaloniaKeyToKey[(int)Key.F15] = Input.Key.F15;
        AvaloniaKeyToKey[(int)Key.F16] = Input.Key.F16;
        AvaloniaKeyToKey[(int)Key.F17] = Input.Key.F17;
        AvaloniaKeyToKey[(int)Key.F18] = Input.Key.F18;
        AvaloniaKeyToKey[(int)Key.F19] = Input.Key.F19;
        AvaloniaKeyToKey[(int)Key.F20] = Input.Key.F20;
        AvaloniaKeyToKey[(int)Key.F21] = Input.Key.F21;
        AvaloniaKeyToKey[(int)Key.F22] = Input.Key.F22;
        AvaloniaKeyToKey[(int)Key.F23] = Input.Key.F23;
        AvaloniaKeyToKey[(int)Key.F24] = Input.Key.F24;
        AvaloniaKeyToKey[(int)Key.NumLock] = Input.Key.NumLock;
        AvaloniaKeyToKey[(int)Key.Scroll] = Input.Key.Scroll;
        AvaloniaKeyToKey[(int)Key.LeftShift] = Input.Key.LeftShift;
        AvaloniaKeyToKey[(int)Key.RightShift] = Input.Key.RightShift;
        AvaloniaKeyToKey[(int)Key.LeftCtrl] = Input.Key.LeftCtrl;
        AvaloniaKeyToKey[(int)Key.RightCtrl] = Input.Key.RightCtrl;
        AvaloniaKeyToKey[(int)Key.LeftAlt] = Input.Key.LeftAlt;
        AvaloniaKeyToKey[(int)Key.RightAlt] = Input.Key.RightAlt;
        AvaloniaKeyToKey[(int)Key.BrowserBack] = Input.Key.BrowserBack;
        AvaloniaKeyToKey[(int)Key.BrowserForward] = Input.Key.BrowserForward;
        AvaloniaKeyToKey[(int)Key.BrowserRefresh] = Input.Key.BrowserRefresh;
        AvaloniaKeyToKey[(int)Key.BrowserStop] = Input.Key.BrowserStop;
        AvaloniaKeyToKey[(int)Key.BrowserSearch] = Input.Key.BrowserSearch;
        AvaloniaKeyToKey[(int)Key.BrowserFavorites] = Input.Key.BrowserFavorites;
        AvaloniaKeyToKey[(int)Key.BrowserHome] = Input.Key.BrowserHome;
        AvaloniaKeyToKey[(int)Key.VolumeMute] = Input.Key.VolumeMute;
        AvaloniaKeyToKey[(int)Key.VolumeDown] = Input.Key.VolumeDown;
        AvaloniaKeyToKey[(int)Key.VolumeUp] = Input.Key.VolumeUp;
        AvaloniaKeyToKey[(int)Key.MediaNextTrack] = Input.Key.MediaNextTrack;
        AvaloniaKeyToKey[(int)Key.MediaPreviousTrack] = Input.Key.MediaPreviousTrack;
        AvaloniaKeyToKey[(int)Key.MediaStop] = Input.Key.MediaStop;
        AvaloniaKeyToKey[(int)Key.MediaPlayPause] = Input.Key.MediaPlayPause;
        AvaloniaKeyToKey[(int)Key.LaunchMail] = Input.Key.LaunchMail;
        AvaloniaKeyToKey[(int)Key.SelectMedia] = Input.Key.SelectMedia;
        AvaloniaKeyToKey[(int)Key.LaunchApplication1] = Input.Key.LaunchApplication1;
        AvaloniaKeyToKey[(int)Key.LaunchApplication2] = Input.Key.LaunchApplication2;
        AvaloniaKeyToKey[(int)Key.OemSemicolon] = Input.Key.OemSemicolon;
        AvaloniaKeyToKey[(int)Key.Oem1] = Input.Key.Oem1;
        AvaloniaKeyToKey[(int)Key.OemPlus] = Input.Key.OemPlus;
        AvaloniaKeyToKey[(int)Key.OemComma] = Input.Key.OemComma;
        AvaloniaKeyToKey[(int)Key.OemMinus] = Input.Key.OemMinus;
        AvaloniaKeyToKey[(int)Key.OemPeriod] = Input.Key.OemPeriod;
        AvaloniaKeyToKey[(int)Key.OemQuestion] = Input.Key.OemQuestion;
        AvaloniaKeyToKey[(int)Key.Oem2] = Input.Key.Oem2;
        AvaloniaKeyToKey[(int)Key.OemTilde] = Input.Key.OemTilde;
        AvaloniaKeyToKey[(int)Key.Oem3] = Input.Key.Oem3;
        AvaloniaKeyToKey[(int)Key.AbntC1] = Input.Key.AbntC1;
        AvaloniaKeyToKey[(int)Key.AbntC2] = Input.Key.AbntC2;
        AvaloniaKeyToKey[(int)Key.OemOpenBrackets] = Input.Key.OemOpenBrackets;
        AvaloniaKeyToKey[(int)Key.Oem4] = Input.Key.Oem4;
        AvaloniaKeyToKey[(int)Key.OemPipe] = Input.Key.OemPipe;
        AvaloniaKeyToKey[(int)Key.Oem5] = Input.Key.Oem5;
        AvaloniaKeyToKey[(int)Key.OemCloseBrackets] = Input.Key.OemCloseBrackets;
        AvaloniaKeyToKey[(int)Key.Oem6] = Input.Key.Oem6;
        AvaloniaKeyToKey[(int)Key.OemQuotes] = Input.Key.OemQuotes;
        AvaloniaKeyToKey[(int)Key.Oem7] = Input.Key.Oem7;
        AvaloniaKeyToKey[(int)Key.Oem8] = Input.Key.Oem8;
        AvaloniaKeyToKey[(int)Key.OemBackslash] = Input.Key.OemBackslash;
        AvaloniaKeyToKey[(int)Key.Oem102] = Input.Key.Oem102;
        AvaloniaKeyToKey[(int)Key.ImeProcessed] = Input.Key.ImeProcessed;
        AvaloniaKeyToKey[(int)Key.System] = Input.Key.System;
        AvaloniaKeyToKey[(int)Key.OemAttn] = Input.Key.OemAttn;
        AvaloniaKeyToKey[(int)Key.DbeAlphanumeric] = Input.Key.DbeAlphanumeric;
        AvaloniaKeyToKey[(int)Key.OemFinish] = Input.Key.OemFinish;
        AvaloniaKeyToKey[(int)Key.DbeKatakana] = Input.Key.DbeKatakana;
        AvaloniaKeyToKey[(int)Key.DbeHiragana] = Input.Key.DbeHiragana;
        AvaloniaKeyToKey[(int)Key.OemCopy] = Input.Key.OemCopy;
        AvaloniaKeyToKey[(int)Key.DbeSbcsChar] = Input.Key.DbeSbcsChar;
        AvaloniaKeyToKey[(int)Key.OemAuto] = Input.Key.OemAuto;
        AvaloniaKeyToKey[(int)Key.DbeDbcsChar] = Input.Key.DbeDbcsChar;
        AvaloniaKeyToKey[(int)Key.OemEnlw] = Input.Key.OemEnlw;
        AvaloniaKeyToKey[(int)Key.OemBackTab] = Input.Key.OemBackTab;
        AvaloniaKeyToKey[(int)Key.DbeRoman] = Input.Key.DbeRoman;
        AvaloniaKeyToKey[(int)Key.DbeNoRoman] = Input.Key.DbeNoRoman;
        AvaloniaKeyToKey[(int)Key.Attn] = Input.Key.Attn;
        AvaloniaKeyToKey[(int)Key.CrSel] = Input.Key.CrSel;
        AvaloniaKeyToKey[(int)Key.DbeEnterWordRegisterMode] = Input.Key.DbeEnterWordRegisterMode;
        AvaloniaKeyToKey[(int)Key.ExSel] = Input.Key.ExSel;
        AvaloniaKeyToKey[(int)Key.DbeEnterImeConfigureMode] = Input.Key.DbeEnterImeConfigureMode;
        AvaloniaKeyToKey[(int)Key.EraseEof] = Input.Key.EraseEof;
        AvaloniaKeyToKey[(int)Key.DbeFlushString] = Input.Key.DbeFlushString;
        AvaloniaKeyToKey[(int)Key.Play] = Input.Key.Play;
        AvaloniaKeyToKey[(int)Key.DbeCodeInput] = Input.Key.DbeCodeInput;
        AvaloniaKeyToKey[(int)Key.DbeNoCodeInput] = Input.Key.DbeNoCodeInput;
        AvaloniaKeyToKey[(int)Key.Zoom] = Input.Key.Zoom;
        AvaloniaKeyToKey[(int)Key.NoName] = Input.Key.NoName;
        AvaloniaKeyToKey[(int)Key.DbeDetermineString] = Input.Key.DbeDetermineString;
        AvaloniaKeyToKey[(int)Key.DbeEnterDialogConversionMode] = Input.Key.DbeEnterDialogConversionMode;
        AvaloniaKeyToKey[(int)Key.Pa1] = Input.Key.Pa1;
        AvaloniaKeyToKey[(int)Key.OemClear] = Input.Key.OemClear;
        AvaloniaKeyToKey[(int)Key.DeadCharProcessed] = Input.Key.DeadCharProcessed;
    }
}