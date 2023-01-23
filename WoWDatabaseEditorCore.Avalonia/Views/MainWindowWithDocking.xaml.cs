using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using AvaloniaStyles;
using AvaloniaStyles.Controls;
using Dock.Avalonia.Controls;
using Newtonsoft.Json;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Tasks;
using WDE.Common.Types;
using WDE.Common.Utils;
using WoWDatabaseEditorCore.Avalonia.Docking;
using WoWDatabaseEditorCore.Avalonia.Docking.Serialization;
using WoWDatabaseEditorCore.ViewModels;

namespace WoWDatabaseEditorCore.Avalonia.Views
{
    public class MainWindowWithDocking : ExtendedWindow
    {
        private static string DockSettingsFile = "~/dock.ava.layout";
        private readonly IFileSystem fileSystem;
        private readonly IUserSettings userSettings;
        private AvaloniaDockAdapter avaloniaDockAdapter;

        public static readonly AttachedProperty<bool> OnEnterPressedProperty = 
            AvaloniaProperty.RegisterAttached<CompletionComboBox, bool>("OnEnterPressed", typeof(MainWindowWithDocking));

        public MainWindowWithDocking()
        {
            fileSystem = null!;
            userSettings = null!;
            avaloniaDockAdapter = null!;
        }
        
        public static bool GetOnEnterPressed(IAvaloniaObject obj)
        {
            return obj.GetValue(OnEnterPressedProperty);
        }

        public static void SetOnEnterPressed(IAvaloniaObject obj, bool value)
        {
            obj.SetValue(OnEnterPressedProperty, value);
        }

        static MainWindowWithDocking()
        {
            OnEnterPressedProperty.Changed.AddClassHandler<CompletionComboBox>((box, args) =>
            {
                box.OnEnterPressed += (sender, pressedArgs) =>
                {
                    var box = (CompletionComboBox)sender!;
                    if (pressedArgs.SelectedItem == null && box.SelectedItem != null && string.IsNullOrEmpty(pressedArgs.SearchText))
                    {
                        var oldItem = box.SelectedItem;
                        box.SelectedItem = null;
                        box.SelectedItem = oldItem; // reselect the same item on subsequent enter press
                        pressedArgs.Handled = true;
                    }
                    else if (pressedArgs.SelectedItem == null && long.TryParse(pressedArgs.SearchText, out var l))
                    {
                        box.SelectedItem = new QuickGoToItemViewModel(l, "(unknown)");
                        pressedArgs.Handled = true;
                    }
                };
            });
        }
        
        public MainWindowWithDocking(IMainWindowHolder mainWindowHolder, 
            IDocumentManager documentManager, 
            ILayoutViewModelResolver layoutViewModelResolver,
            IFileSystem fileSystem,
            IUserSettings userSettings)
        {
            this.fileSystem = fileSystem;
            this.userSettings = userSettings;
            avaloniaDockAdapter = new AvaloniaDockAdapter(documentManager, layoutViewModelResolver);
            
            // we have to do it before InitializeComponent!
            var primaryScreen = Screens.Primary ?? Screens.All.FirstOrDefault();
            GlobalApplication.HighDpi = (primaryScreen?.PixelDensity ?? 1) > 1.5f;
            
            InitializeComponent();
            this.AttachDevTools();
            mainWindowHolder.Window = this;
            if (SystemTheme.EffectiveTheme is SystemThemeOptions.LightWindows11 or SystemThemeOptions.DarkWindows11)
            {
                this.Classes.Add("win11");
            }
            else
            {
                this.Classes.Add("win10");
            }
            PersistentDockDataTemplate.DocumentManager = documentManager;
        }

        public static ImageUri DocumentIcon => new ImageUri("Icons/document.png");

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            AddHandler(KeyUpEvent, OnKeyUpTunneled, RoutingStrategies.Tunnel);
            
            DockControl dock = this.FindControl<DockControl>("DockControl");
            
            SerializedDock? serializedDock = null;
            if (fileSystem.Exists(DockSettingsFile))
            {
                try
                {
                    serializedDock = JsonConvert.DeserializeObject<SerializedDock>(fileSystem.ReadAllText(DockSettingsFile));
                } catch {}
            }

            dock.Layout = avaloniaDockAdapter.Initialize(serializedDock);

            var lastSize = userSettings.Get<WindowLastSize>();
            
            if (lastSize.LastX.HasValue && lastSize.LastY.HasValue && lastSize.LastWidth.HasValue &&
                lastSize.LastHeight.HasValue)
            {
                var p = new PixelPoint(lastSize.LastX.Value + 20, lastSize.LastY.Value + 20);
                var screen = Screens.ScreenFromPoint(p);
                if (screen != null)
                {
                    Position = new PixelPoint(lastSize.LastX.Value, lastSize.LastY.Value);
                    if (!(lastSize.WasMaximized ?? false))
                    {
                        var tl = this.PointToClient(new PixelPoint(lastSize.LastX.Value, lastSize.LastY.Value));
                        var br = this.PointToClient(new PixelPoint(lastSize.LastX.Value + lastSize.LastWidth.Value, lastSize.LastY.Value + lastSize.LastHeight.Value));
                        Width = Math.Max(br.X - tl.X, 100);
                        Height = Math.Max(br.Y - tl.Y, 100);   
                    }
                }
            }
            WindowState = (lastSize.WasMaximized ?? false) ? WindowState.Maximized : WindowState.Normal;
        }

        private void OnKeyUpTunneled(object? sender, KeyEventArgs e)
        {
            // I can't do it in KeyBindings, because it needs to be handled in tunnel handler
            // also don't do it in OnKeyDown, because it is not fired for control + tab
            if (e.KeyModifiers.HasFlagFast(KeyModifiers.Control) &&
                e.Key == Key.Tab)
            {
                var documentControl = FocusManager.Instance.Current?.FindAncestorOfType<DocumentControl>() ?? this.FindDescendantOfType<DocumentControl>();
                if (documentControl?.DataContext is not FocusAwareDocumentDock documentDock)
                    return;
                    
                if (documentDock.ActiveDockable == null || documentDock.VisibleDockables == null)
                    return;
                    
                var activeDockableIndex = documentDock.VisibleDockables.IndexOf(documentDock.ActiveDockable);
                if (activeDockableIndex == -1)
                    return;

                var direction = e.KeyModifiers.HasFlagFast(KeyModifiers.Shift) ? -1 : 1;

                activeDockableIndex = activeDockableIndex + direction;
                if (activeDockableIndex == documentDock.VisibleDockables.Count)
                    activeDockableIndex = 0;
                if (activeDockableIndex == -1)
                    activeDockableIndex = documentDock.VisibleDockables.Count - 1;

                documentDock.ActiveDockable = documentDock.VisibleDockables[activeDockableIndex];
                
                e.Handled = true;
            }
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            if (DataContext is ICloseAwareViewModel closeAwareViewModel)
            {
                closeAwareViewModel.CloseRequest += Close;
                closeAwareViewModel.ForceCloseRequest += () =>
                {
                    realClosing = true;
                    Close();
                };
            }
        }

        private bool realClosing = false;
        
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            // we use screen coords for size so that size is custom app scaling independent 
            var screenTopLeftPoint = this.PointToScreen(new Point(Position.X, Position.Y));
            var screenBottomRightPoint = this.PointToScreen(new Point(Position.X + this.ClientSize.Width, Position.Y + this.ClientSize.Height));
            userSettings.Update(new WindowLastSize()
            {
                WasMaximized = WindowState == WindowState.Maximized || WindowState == WindowState.FullScreen,
                LastX = Position.X,
                LastY = Position.Y,
                LastWidth = screenBottomRightPoint.X - screenTopLeftPoint.X,
                LastHeight = screenBottomRightPoint.Y - screenTopLeftPoint.Y,
            });
            fileSystem.WriteAllText(DockSettingsFile,
                JsonConvert.SerializeObject(avaloniaDockAdapter.SerializeDock(), Formatting.Indented));
            if (DataContext is MainWindowViewModel closeAwareViewModel)
            {
                if (!realClosing)
                {
                    TryClose(closeAwareViewModel).ListenErrors();
                    e.Cancel = true;
                }
                else
                    closeAwareViewModel.NotifyWillClose();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            GlobalApplication.IsRunning = false;
        }
        
        private async Task TryClose(MainWindowViewModel closeAwareViewModel)
        {
            if (await closeAwareViewModel.CanClose())
            {
                realClosing = true;
                Close();
            }
        }

        public struct WindowLastSize : ISettings
        {
            public bool? WasMaximized { get; set; }
            public int? LastX { get; set; }
            public int? LastY { get; set; }
            public int? LastWidth { get; set; }
            public int? LastHeight { get; set; }
        }
    }
}