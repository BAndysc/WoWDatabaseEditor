using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using TheEngine;
using WDE.Common.Utils;
using WDE.MapSpawnsEditor.ViewModels;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WDE.MapSpawnsEditor.Views;

public partial class SpawnsToolView : UserControl
{
    private SpawnsFastTreeList spawns = null!;
    private System.IDisposable? disposable;
    
    public SpawnsToolView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        spawns = this.GetControl<SpawnsFastTreeList>("SpawnsList");
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        disposable = ((SpawnsToolViewModel)DataContext!)!.ToObservable(x => x.SearchText)
            .SubscribeAction(_ =>
            {
                spawns.InvalidateMeasure();
                spawns.InvalidateVisual();
            });
        ((SpawnsToolViewModel)DataContext!).FocusRequest += OnFocusRequest;
    }

    private void OnFocusRequest()
    {
        var root = this.GetVisualRoot() as TopLevel;
        var panel = root?.FindDescendantOfType<NativeTheEnginePanel>();
        
        Dispatcher.UIThread.Post(() =>
        {
            panel!.Focus(NavigationMethod.Tab);
        }, DispatcherPriority.Render);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        ((SpawnsToolViewModel)DataContext!).FocusRequest -= OnFocusRequest;
        disposable?.Dispose();
        disposable = null;
    }

    private void InputElement_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is SpawnsToolViewModel vm && vm.SelectedNode is SpawnInstance selectedSpawn)
        {
            vm.TeleportTo(selectedSpawn).ListenErrors();
            e.Handled = true;
        }
    }

    private void SpawnsList_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled)
            return;

        if (e.Key == Key.Space || e.Key == Key.Enter)
        {
            if (DataContext is SpawnsToolViewModel vm && vm.SelectedNode is SpawnInstance selectedSpawn)
            {
                vm.TeleportTo(selectedSpawn).ListenErrors();
                e.Handled = true;
            }
        }
    }
}
