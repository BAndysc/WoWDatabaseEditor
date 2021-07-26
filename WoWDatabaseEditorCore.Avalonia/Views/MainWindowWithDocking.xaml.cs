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
using WoWDatabaseEditorCore.Avalonia.Docking;
using WoWDatabaseEditorCore.Avalonia.Docking.Serialization;
using WoWDatabaseEditorCore.ViewModels;

namespace WoWDatabaseEditorCore.Avalonia.Views
{
    public class MainWindowWithDocking : ExtendedWindow
    {
        private static string DockSettingsFile = "~/dock.ava.layout";
        private readonly IFileSystem fileSystem;
        private AvaloniaDockAdapter avaloniaDockAdapter;

        public MainWindowWithDocking()
        {
            fileSystem = null!;
            avaloniaDockAdapter = null!;
        }
        
        public MainWindowWithDocking(IMainWindowHolder mainWindowHolder, 
            IDocumentManager documentManager, 
            ILayoutViewModelResolver layoutViewModelResolver,
            IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
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
            fileSystem.WriteAllText(DockSettingsFile,
                JsonConvert.SerializeObject(avaloniaDockAdapter.SerializeDock(), Formatting.Indented));
            if (!realClosing && DataContext is MainWindowViewModel closeAwareViewModel)
            {
                TryClose(closeAwareViewModel);
                e.Cancel = true;
            }
        }

        private async Task TryClose(MainWindowViewModel closeAwareViewModel)
        {
            if (await closeAwareViewModel.CanClose())
            {
                realClosing = true;
                Close();
            }
        }
    }
}