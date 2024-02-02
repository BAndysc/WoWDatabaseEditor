using System;
using System.Collections.Generic;
using System.Threading;
using AsyncAwaitBestPractices.MVVM;
using PropertyChanged.SourceGenerator;
using WDE.Common.Debugging;
using WDE.Common.Exceptions;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MVVM;

namespace WDE.Debugger.ViewModels.Inspector;

[AutoRegister]
internal partial class DebugPointsInspectorViewModel : ObservableBase, IWindowViewModel
{
    private readonly IRemoteConnectorService remoteConnectorService;
    private readonly IDebuggerService debuggerService;
    public IAsyncCommand<AddDebugPointViewModel> AddDebugPointCommand { get; }
    public IAsyncCommand DeleteSelectedDebugPointsCommand { get; }
    public IAsyncCommand FetchDebugPointsCommand { get; }
    public IAsyncCommand ClearAllDebugPointsCommand { get; }

    [Notify] [AlsoNotify(nameof(SelectedItem))] private INodeType? selectedNode;

    public InspectorDebugPointViewModel? SelectedItem => selectedNode as InspectorDebugPointViewModel;

    public List<InspectorDebugSourceViewModel> Sources { get; } = new();

    public List<AddDebugPointViewModel> SourceAddList { get; } = new();

    private Dictionary<IDebugPointSource, InspectorDebugSourceViewModel> sourceToViewModel = new();

    private Dictionary<DebugPointId, InspectorDebugPointViewModel> pointToViewModel = new();

    public FlatTreeList<InspectorDebugSourceViewModel, InspectorDebugPointViewModel> FlatItems { get; }

    private SelectedDebugPointViewModel? selectedDebugPointViewModel;
    public SelectedDebugPointViewModel? SelectedDebugPointViewModel
    {
        get => selectedDebugPointViewModel;
        set
        {
            if (selectedDebugPointViewModel != value)
            {
                selectedDebugPointViewModel?.Dispose();
                selectedDebugPointViewModel = value;
                RaisePropertyChanged();
            }
        }
    }

    public DebugPointsInspectorViewModel(IRemoteConnectorService remoteConnectorService,
        IDebuggerService debuggerService,
        IMessageBoxService messageBoxService)
    {
        this.remoteConnectorService = remoteConnectorService;
        this.debuggerService = debuggerService;
        debuggerService.DebugPointAdded += OnDebugPointAdded;
        debuggerService.DebugPointChanged += OnDebugPointChanged;
        debuggerService.DebugPointRemoving += OnDebugPointRemoved;

        foreach (var source in debuggerService.Sources)
        {
            var viewModel = new InspectorDebugSourceViewModel(source);
            Sources.Add(viewModel);
            sourceToViewModel.Add(source, viewModel);
            if (source.Features.HasFlagFast(DebugSourceFeatures.CanCreateDebugPoint))
                SourceAddList.Add(new AddDebugPointViewModel(source));
        }

        foreach (var debugPoint in this.debuggerService.DebugPoints)
            OnDebugPointAdded(debugPoint);

        FlatItems = new FlatTreeList<InspectorDebugSourceViewModel, InspectorDebugPointViewModel>(Sources);

        AddDebugPointCommand = new AsyncAutoCommand<AddDebugPointViewModel>(async x =>
        {
            await x.Source.CreateDebugPoint();
        }).WrapMessageBox<Exception, AddDebugPointViewModel>(messageBoxService);
        DeleteSelectedDebugPointsCommand = new AsyncCommand(async () =>
        {
            if (SelectedItem != null)
                await debuggerService.RemoveDebugPointAsync(SelectedItem.Id);
        }, _ => SelectedItem != null).WrapMessageBox<Exception>(messageBoxService);
        On(() => SelectedItem, _ => DeleteSelectedDebugPointsCommand.RaiseCanExecuteChanged());
        FetchDebugPointsCommand = new AsyncAutoCommand(async () =>
        {
            if (!this.remoteConnectorService.IsConnected)
                throw new UserException("Not connected to the server", "Can't fetch breakpoints, because there is no server connection.");

            foreach (var source in this.debuggerService.Sources)
                await source.FetchFromServerAsync(CancellationToken.None);
        }).WrapMessageBox<Exception>(messageBoxService);

        ClearAllDebugPointsCommand = new AsyncAutoCommand(async () =>
        {
            await debuggerService.ClearAllDebugPoints();
        }).WrapMessageBox<Exception>(messageBoxService);

        On(() => SelectedItem, item =>
        {
            if (item != null)
                SelectedDebugPointViewModel = new SelectedDebugPointViewModel(this.debuggerService, new[]{item.Id});
            else
                SelectedDebugPointViewModel = null;
        });
    }

    private void OnDebugPointAdded(DebugPointId obj)
    {
        var source = debuggerService.GetSource(obj);
        var viewModel = sourceToViewModel[source];

        var item = new InspectorDebugPointViewModel(obj, debuggerService);
        viewModel.DebugPoints.Add(item);
        pointToViewModel.Add(obj, item);
    }

    private void OnDebugPointRemoved(DebugPointId obj)
    {
        var source = debuggerService.GetSource(obj);
        var viewModel = sourceToViewModel[source];
        var itemViewModel = pointToViewModel[obj];
        pointToViewModel.Remove(obj);
        viewModel.DebugPoints.Remove(itemViewModel);
        if (SelectedItem?.Id == obj)
            SelectedNode = null;
    }

    private void OnDebugPointChanged(DebugPointId obj)
    {
        var source = debuggerService.GetSource(obj);
        var viewModel = sourceToViewModel[source];
        var itemViewModel = pointToViewModel[obj];
        itemViewModel.RaiseChanged();
    }

    public override void Dispose()
    {
        debuggerService.DebugPointAdded -= OnDebugPointAdded;
        debuggerService.DebugPointChanged -= OnDebugPointChanged;
        debuggerService.DebugPointRemoving -= OnDebugPointRemoved;
        selectedDebugPointViewModel?.Dispose();
        base.Dispose();
    }

    public int DesiredWidth => 800;
    public int DesiredHeight => 550;
    public string Title => "Breakpoints";
    public bool Resizeable => true;
    public ImageUri? Icon => new ImageUri("Icons/icon_mini_breakpoint_all.png");
}