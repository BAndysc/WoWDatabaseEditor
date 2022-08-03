using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
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

        public MainWindowWithDocking()
        {
            fileSystem = null!;
            userSettings = null!;
            avaloniaDockAdapter = null!;
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
            PersistentDockDataTemplate.DocumentManager = documentManager;
        }

        public static ImageUri DocumentIcon => new ImageUri("Icons/document.png");

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
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
            if (!realClosing && DataContext is MainWindowViewModel closeAwareViewModel)
            {
                TryClose(closeAwareViewModel).ListenErrors();
                e.Cancel = true;
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