using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaStyles;
using Dock.Avalonia.Controls;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WoWDatabaseEditorCore.Avalonia.Docking;
using WoWDatabaseEditorCore.Settings;
using WoWDatabaseEditorCore.ViewModels;

namespace WoWDatabaseEditorCore.Avalonia.Views;

public partial class MainWebView : UserControl
{
    private AvaloniaDockAdapter avaloniaDockAdapter;

    public MainWebView()
    {
        avaloniaDockAdapter = null!;
    }

    public MainWebView(
        IDocumentManager documentManager, 
        ILayoutViewModelResolver layoutViewModelResolver,
        IFileSystem fileSystem,
        IUserSettings userSettings,
        TempToolbarButtonStyleService tempToolbarButtonStyleService)
    {
        avaloniaDockAdapter = new AvaloniaDockAdapter(documentManager, layoutViewModelResolver);
        PersistentDockDataTemplate.DocumentManager = documentManager;
        InitializeComponent();

        if (SystemTheme.EffectiveTheme is SystemThemeOptions.LightWindows11 or SystemThemeOptions.DarkWindows11)
        {
            this.Classes.Add("win11");
        }
        else
        {
            this.Classes.Add("win10");
        }

        tempToolbarButtonStyleService.ToObservable(x => x.Style)
            .SubscribeAction(style =>
            {
                Application.Current!.Resources["DisplayButtonImageIcon"] = style is ToolBarButtonStyle.Icon or ToolBarButtonStyle.IconAndText;
                Application.Current!.Resources["DisplayButtonImageText"] = style is ToolBarButtonStyle.Text or ToolBarButtonStyle.IconAndText;
            });
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        DockControl dock = this.GetControl<DockControl>("DockControl");
        dock.Layout = avaloniaDockAdapter.Initialize(null);
    }
}