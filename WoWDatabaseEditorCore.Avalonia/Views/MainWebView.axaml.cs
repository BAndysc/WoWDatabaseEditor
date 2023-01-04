using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Dock.Avalonia.Controls;
using WDE.Common.Managers;
using WDE.Common.Services;
using WoWDatabaseEditorCore.Avalonia.Docking;
using WoWDatabaseEditorCore.ViewModels;

namespace WoWDatabaseEditorCore.Avalonia.Views;

public partial class MainWebView : UserControl
{
    private AvaloniaDockAdapter avaloniaDockAdapter;
    public MainWebView(
        IDocumentManager documentManager, 
        ILayoutViewModelResolver layoutViewModelResolver,
        IFileSystem fileSystem,
        IUserSettings userSettings)
    {
        avaloniaDockAdapter = new AvaloniaDockAdapter(documentManager, layoutViewModelResolver);
        PersistentDockDataTemplate.DocumentManager = documentManager;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        DockControl dock = this.GetControl<DockControl>("DockControl");
        dock.Layout = avaloniaDockAdapter.Initialize(null);
    }
}