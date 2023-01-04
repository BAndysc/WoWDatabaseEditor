using System;
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
        Console.WriteLine("MainWebView ctor");
        avaloniaDockAdapter = new AvaloniaDockAdapter(documentManager, layoutViewModelResolver);
        PersistentDockDataTemplate.DocumentManager = documentManager;
        Console.WriteLine("MainWebView ctor2");
        InitializeComponent();
        Console.WriteLine("MainWebView ctor3");
    }

    private void InitializeComponent()
    {
        Console.WriteLine("MainWebView InitializeComponent");
        AvaloniaXamlLoader.Load(this);
        Console.WriteLine("MainWebView InitializeComponent2");
        DockControl dock = this.GetControl<DockControl>("DockControl");
        dock.Layout = avaloniaDockAdapter.Initialize(null);
        Console.WriteLine("MainWebView InitializeComponent3");
    }
}