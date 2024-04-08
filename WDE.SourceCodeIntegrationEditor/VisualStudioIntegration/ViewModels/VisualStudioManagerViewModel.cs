using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;
using EnvDTE;
using Prism.Events;
using Prism.Ioc;
using WDE.Common;
using WDE.Common.Debugging;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.SourceCodeIntegrationEditor.VisualStudioIntegration.DevelopmentToolsEnvironments;
using WDE.SourceCodeIntegrationEditor.VisualStudioIntegration.Services;
using WDE.SourceCodeIntegrationEditor.VisualStudioIntegration.Views;
using Window = Avalonia.Controls.Window;

namespace WDE.SourceCodeIntegrationEditor.VisualStudioIntegration.ViewModels;

[AutoRegister]
[SingleInstance]
internal class VisualStudioManagerViewModel : IVisualStudioManagerViewModel
{
    private readonly IRemoteConnectorService remoteConnectorService;
    private readonly IVisualStudioProvider visualStudioProvider;
    private readonly IContainerProvider containerProvider;
    private readonly IEventAggregator eventAggregator;
    private readonly ISourceCodePathService sourceCodePathService;

    private List<IDTE> connectedInstances = new();

    public ObservableCollection<BreakpointHitPopupViewModel> LoosePopups { get; } = new();

    public VisualStudioManagerViewModel(IRemoteConnectorService remoteConnectorService,
        IVisualStudioProvider visualStudioProvider,
        IContainerProvider containerProvider,
        IEventAggregator eventAggregator,
        ISourceCodePathService sourceCodePathService)
    {
        this.remoteConnectorService = remoteConnectorService;
        this.visualStudioProvider = visualStudioProvider;
        this.containerProvider = containerProvider;
        this.eventAggregator = eventAggregator;
        this.sourceCodePathService = sourceCodePathService;
        this.remoteConnectorService.EditorConnected += () => EditorConnected().ListenErrors();
        this.remoteConnectorService.EditorDisconnected += EditorDisconnected;
        if (this.remoteConnectorService.IsConnected)
            EditorConnected().ListenErrors();
    }

    private void EditorDisconnected()
    {
        if (connectedInstances.Count > 0)
        {
            for (var index = connectedInstances.Count - 1; index >= 0; index--)
            {
                var x = connectedInstances[index];
                DisconnectFromVisualStudioInstance(x);
            }
        }
        Debug.Assert(connectedInstances.Count == 0);
        connectedInstances.ForEach(x => x.DisposeAsync().AsTask().ListenErrors());
        connectedInstances.Clear();
    }

    private async Task EditorConnected()
    {
        EditorDisconnected();

        var instances = visualStudioProvider.GetRunningInstances();
        foreach (var instance in instances)
        {
            try
            {
                if (!instance.SkipSolutionPathValidation)
                {
                    var solutionPath = await instance.GetSolutionFullPath();
                    if (solutionPath == null)
                        continue;

                    var solutionPathUri = new FileInfo(solutionPath);

                    if (!sourceCodePathService.SourceCodePaths.Any(dir => solutionPathUri.FullName.StartsWith(dir)))
                        continue;
                }

                await instance.ConnectAsync();

                ConnectToVisualStudioInstance(instance);
            }
            catch (Exception e)
            {
                LOG.LogError(e);
            }
        }
    }

    private void ConnectToVisualStudioInstance(IDTE instance)
    {
        connectedInstances.Add(instance);
        instance.BreakModeEntered += BreakpointHit;
        instance.RunModeEntered += RuntimeContinued;
    }

    private void DisconnectFromVisualStudioInstance(IDTE instance)
    {
        instance.DisposeAsync().AsTask().ListenErrors();
        instance.BreakModeEntered -= BreakpointHit;
        instance.RunModeEntered -= RuntimeContinued;
        connectedInstances.Remove(instance);
    }

    private void RuntimeContinued(object? sender, dbgEventReason reason)
    {
        eventAggregator.GetEvent<IdeBreakpointResumeEvent>().Publish();
        foreach (var popup in popups)
            popup.Close();
        popups.Clear();

        foreach (var vm in LoosePopups)
        {
            vm.CloseRequested -= OnViewModelClosedRequested;
            vm.Dispose();
        }
        LoosePopups.Clear();
    }

    private void BreakpointHit(object? sender, IdeBreakpointHitEventArgs args)
    {
        eventAggregator.GetEvent<IdeBreakpointHitEvent>().Publish(args);

        if (sender is not IDTE dte)
            return;

        if (args.HitDebugPoint == DebugPointId.Empty)
            return;

        var e = new IdeBreakpointRequestPopupEventArgs(args.HitDebugPoint);
        eventAggregator.GetEvent<IdeBreakpointRequestPopupEvent>().Publish(e);
        if (e.AttachPopupToObject is Control { } c && c.GetVisualRoot() is { } root)
        {
            var vm = new BreakpointHitPopupViewModel(dte, args) { ShowTail = true };
            var view = new BreakpointHitPopupView() { DataContext = vm };
            OpenAsAdornerAndDispose(vm, view, c, e.PopupOffsetX, e.PopupOffsetY).ListenErrors();
            popups.Add(view);
            if (root is Window window)
                window.Activate();
        }
        else
        {
            var vm = new BreakpointHitPopupViewModel(dte, args) { ShowTail = false };
            vm.CloseRequested += OnViewModelClosedRequested;
            LoosePopups.Add(vm);
        }
    }

    private void OnViewModelClosedRequested(BreakpointHitPopupViewModel popup)
    {
        LoosePopups.Remove(popup);
        popup.CloseRequested -= OnViewModelClosedRequested;
    }

    private async Task OpenAsAdornerAndDispose(BreakpointHitPopupViewModel vm, BreakpointHitPopupView view, Control owner, double offsetX, double offsetY)
    {
        await view.OpenAsAdorner(owner, offsetX, offsetY);
        vm.Dispose();
    }

    private List<BreakpointHitPopupView> popups = new();
}