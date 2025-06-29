using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AsyncAwaitBestPractices.MVVM;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Nodify;
using Nodify.Compatibility;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.Common.Utils;
using WDE.MVVM.Observable;
using WDE.QuestChainEditor.Models;
using WDE.QuestChainEditor.ViewModels;

namespace WDE.QuestChainEditor.Views;

public partial class QuestChainDocumentView : UserControl
{
    private ItemContainer? currentOverNode = null;

    private int currentZIndexMax = 0;

    private System.IDisposable? timer;

    public QuestChainDocumentViewModel ViewModel => (QuestChainDocumentViewModel)DataContext!;

    private AsyncCommand<TeleDestination> TeleportCommand { get; }

    private CancellationTokenSource? teleportDestinationCancellation;

    public QuestChainDocumentView()
    {
        InitializeComponent();
        Editor.AddHandler(ItemContainer.DragStartedEvent, OnItemsDragStarted, RoutingStrategies.Tunnel);
        Editor.AddHandler(ItemContainer.DragDeltaEvent, OnItemsDragDelta, RoutingStrategies.Tunnel);
        Editor.AddHandler(ItemContainer.DragCompletedEvent, OnItemsDragCompleted, RoutingStrategies.Bubble);
        Editor.AddHandler(ItemContainer.SelectedEvent, OnSelectedItem, RoutingStrategies.Bubble);
        Editor.AddHandler(NodifyEditor.BeforeDraggingStartedEvent, OnBeforeDraggingStarted, RoutingStrategies.Bubble);
        Editor.AddHandler(Connector.PendingConnectionStartedEvent, OnPendingConnectionStarted, RoutingStrategies.Bubble);
        Editor.AddHandler(Connector.PendingConnectionCompletedEvent, OnPendingConnectionCompleted, RoutingStrategies.Bubble);
        Editor.AddHandler(Connector.PendingConnectionConnectorOverrideEvent, OnEndConnectorOverride, RoutingStrategies.Bubble);

        SearchBox.GetObservable(IsVisibleProperty).SubscribeAction(@is =>
        {
            if (@is)
            {
                SearchTextBox.SelectAll();
                SearchTextBox.Focus();
            }
            else
            {
                if (SearchTextBox.IsFocused)
                    Editor.Focus();
            }
        });

        TeleportCommand = new AsyncCommand<TeleDestination>(async (dest) =>
        {
            if (DataContext is not QuestChainDocumentViewModel vm)
                return;

            var playerName = await vm.PickPlayerName();

            if (playerName == null)
                return;

            await vm.QuestChainsServerIntegrationService.TeleportPlayer(playerName, dest!.MapId, dest.X, dest.Y, dest.Z, dest.O);
        });

        TeleportToQuestStarterMenu.GetObservable(MenuItem.IsSubMenuOpenProperty)
            .SubscribeAction(@isOpen =>
            {
                if (@isOpen)
                {
                    teleportDestinationCancellation?.Cancel();
                    teleportDestinationCancellation = new();
                    LoadQuestStarters(TeleportToQuestStarterMenu, teleportDestinationCancellation.Token).ListenErrors();
                }
            });

        TeleportToQuestEnderMenu.GetObservable(MenuItem.IsSubMenuOpenProperty)
            .SubscribeAction(@isOpen =>
            {
                if (@isOpen)
                {
                    teleportDestinationCancellation?.Cancel();
                    teleportDestinationCancellation = new();
                    LoadQuestEnders(TeleportToQuestEnderMenu, teleportDestinationCancellation.Token).ListenErrors();
                }
                else
                {
                    teleportDestinationCancellation?.Cancel();
                    TeleportToQuestEnderMenu.Items.Clear();
                    TeleportToQuestEnderMenu.Items.Add(new MenuItem() { Header = "(loading)" });
                }
            });
    }

    private void OnPendingConnectionCompleted(object? sender, PendingConnectionEventArgs e)
    {
        if (DataContext is not QuestChainDocumentViewModel rootViewModel)
            return;

        rootViewModel.ShiftAltConnectionMessageVisible = false;
    }

    private void OnPendingConnectionStarted(object? sender, PendingConnectionEventArgs e)
    {
        if (DataContext is not QuestChainDocumentViewModel rootViewModel)
            return;

        rootViewModel.ShiftAltConnectionMessageVisible = false;

        rootViewModel.PendingConnection.RequirementType = e.KeyModifiers == 0
            ? ConnectionType.Completed
            : (e.KeyModifiers == KeyModifiers.Control
                ? ConnectionType.Breadcrumb
                : ConnectionType.FactionChange);
    }

    private void OnEndConnectorOverride(object? sender, PendingConnectionConnectorOverrideEventArgs e)
    {
        if (DataContext is not QuestChainDocumentViewModel vm)
            return;

        // if pending connection is breadcrumb OR faction change, then connection to group is not allowed, no matter if user holds shift or not
        if ((vm.PendingConnection.RequirementType == ConnectionType.Breadcrumb ||
             vm.PendingConnection.RequirementType == ConnectionType.FactionChange) &&
                 e.PotentialConnector != null &&
                 e.PotentialConnector.DataContext is ExclusiveGroupViewModel)
        {
            vm.ShiftAltConnectionMessageVisible = false;
            e.PotentialConnector = null;
            return;
        }

        // if KeyModifiers != 0, then allow precise connection
        if (e.KeyModifiers == 0 && Editor is {} editor && e.PotentialConnector != null)
        {
            // no connection to the same group
            if (e.SourceConnector is ExclusiveGroupViewModel fromGroup &&
                (e.PotentialConnector.DataContext is QuestViewModel qvm &&
                qvm.ExclusiveGroup == fromGroup ||
                e.PotentialConnector.DataContext is ExclusiveGroupViewModel toGroup &&
                toGroup == fromGroup))
            {
                vm.ShiftAltConnectionMessageVisible = true;
                e.PotentialConnector = null;
                return;
            }

            // connection to quest in group goes to the group unless pending connection is breadcrumb
            if (vm.PendingConnection.RequirementType != ConnectionType.Breadcrumb &&
                e.PotentialConnector.DataContext is QuestViewModel qvm2 &&
                qvm2.ExclusiveGroup != null &&
                editor.ContainerFromItem(qvm2.ExclusiveGroup) is { } groupContainer &&
                groupContainer.FindDescendantOfType<Connector>() is { } groupConnector)
            {
                vm.ShiftAltConnectionMessageVisible = true;
                e.PotentialConnector = groupConnector;
                return;
            }
        }

        if (e.PotentialConnector != null &&
            e.PotentialConnector is not Connector c &&
            e.PotentialConnector.FindDescendantOfType<Connector>() is { } connector)
        {
            vm.ShiftAltConnectionMessageVisible = false; /* this is just a fixup when the potential connector is ItemContainer */
            e.PotentialConnector = connector;
            return;
        }

        vm.ShiftAltConnectionMessageVisible = false;
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);

        if (e.Handled)
            return;

        if (e.Text == " ")
        {
            LoadQuest_MenuItemClick(default, default!);
        }
    }

    private ItemContainer? GetPointerOverItemContainerHelper(bool allowGroupingNode)
    {
        foreach (var element in Editor.GetRealizedContainers())
        {
            if (element is not ItemContainer container)
                continue;

            if (container.IsSelected)
                continue;

            var bounds = container.Bounds;
            if (bounds.Contains(Editor.MouseLocation))
            {
                if (allowGroupingNode && container.DataContext is QuestViewModel quest &&
                    quest.ExclusiveGroup is { } parentQuestGroup)
                {
                    if (Editor.ContainerFromItem(parentQuestGroup) is ItemContainer parentContainer &&
                        !parentContainer.IsSelected)
                        return parentContainer;
                    return null;
                }
                if (!allowGroupingNode && container.DataContext is ExclusiveGroupViewModel)
                    continue;

                return container;
            }
        }

        return null;
    }

    private void OnBeforeDraggingStarted(object? sender, BeforeDraggingStartedEventArgs e)
    {
        e.AdditionalItemsToDrag ??= new List<ItemContainer>();
        foreach (var selected in Editor.InternalSelectedItems)
        {
            if (selected is ExclusiveGroupViewModel group)
            {
                foreach (var quest in group.Quests)
                {
                    if (Editor.ContainerFromItem(quest) is ItemContainer ic && !ic.IsSelected)
                    {
                        e.AdditionalItemsToDrag.Add(ic);
                    }
                }
            }
        }
    }

    private void OnSelectedItem(object? sender, RoutedEventArgs e)
    {
        int max = Editor.GetRealizedContainers().Max(x => x.ZIndex);
        currentZIndexMax = max + 1;
        foreach (var item in Editor.InternalSelectedItems)
        {
            if (item != null && Editor.ContainerFromItem(item) is { } container)
            {
                if (container.DataContext is not ExclusiveGroupViewModel) // exclusive group should be always on bottom
                    container.ZIndex = max + 1;
            }
        }
    }

    private void OnItemsDragStarted(object? sender, DragStartedEventArgs e)
    {
        UpdateIsDraggingForSelected(true);
    }

    private void OnItemsDragDelta(object? sender, DragDeltaEventArgs e)
    {
        var over = GetPointerOverItemContainerHelper(true);
        if (currentOverNode != over)
        {
            if (currentOverNode != null)
            {
                currentOverNode.Classes.Remove("groupable");
                if (currentOverNode.DataContext is not ExclusiveGroupViewModel) // exclusive group should be always on bottom
                    currentOverNode.ZIndex = currentZIndexMax - 1;
            }

            if (over != null)
            {
                over.Classes.Add("groupable");
                if (over.DataContext is not ExclusiveGroupViewModel) // exclusive group should be always on bottom
                    over.ZIndex = currentZIndexMax + 1;
            }

            currentOverNode = over;
        }
    }

    private void UpdateIsDraggingForSelected(bool isDragging)
    {
        foreach (var selectedItem in Editor.InternalSelectedItems)
        {
            if (selectedItem is ExclusiveGroupViewModel group)
            {
                group.IsDragging = isDragging;
                foreach (var quest in group.Quests)
                    quest.IsDragging = isDragging;
            }
            else if (selectedItem is QuestViewModel quest)
                quest.IsDragging = isDragging;
        }
    }

    private void OnItemsDragCompleted(object? sender, DragCompletedEventArgs e)
    {
        void UpdateLocationHistory()
        {
            if (!ViewModel.AutoLayout && Math.Abs(e.HorizontalChange) + Math.Abs(e.VerticalChange) > 0.5)
            {
                var change = new Point(e.HorizontalChange, e.VerticalChange);
                var affectedNodes = Editor.InternalSelectedItems.Cast<BaseQuestViewModel>()
                    .Select(node => (node.Location, node))
                    .ToList();
                ViewModel.HistoryHandler.PushAction(new AnonymousHistoryAction("Move nodes", () =>
                {
                    foreach (var (position, node) in affectedNodes)
                    {
                        node.PerfectX = position.X - change.X;
                        node.PerfectY = position.Y - change.Y;
                        node.Location = position - change;
                    }
                }, () =>
                {
                    foreach (var (position, node) in affectedNodes)
                    {
                        node.PerfectX = position.X;
                        node.PerfectY = position.Y;
                        node.Location = position;
                    }
                }));
            }
        }

        UpdateIsDraggingForSelected(false);

        var destinationContainer = GetPointerOverItemContainerHelper(true);

        if (destinationContainer == null)
            ViewModel.DegroupSelected();

        UpdateLocationHistory(); // keep it after degrouping and before grouping

        if (destinationContainer?.DataContext is ExclusiveGroupViewModel group)
            ViewModel.GroupSelectedQuests(group);
        else if (destinationContainer?.DataContext is QuestViewModel otherQuest)
            ViewModel.CreateAndGroupSelectedQuests(otherQuest);

        foreach (var selected in Editor.InternalSelectedItems)
        {
            if (selected is ExclusiveGroupViewModel vm)
                vm.Arrange(default);
            else if (selected is QuestViewModel q)
                q.ExclusiveGroup?.Arrange(default);
        }

        if (currentOverNode != null)
        {
            currentOverNode.Classes.Remove("groupable");
        }

        currentOverNode = null;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        timer?.Dispose();
        timer = DispatcherTimer.Run(RelayoutGraph, TimeSpan.FromMilliseconds(16));
        BindToViewModel();
    }

    private QuestChainDocumentViewModel? boundViewModel;

    private void UnBindFromViewModel()
    {
        if (boundViewModel != null)
        {
            boundViewModel.NavigateToQuest -= NavigateToQuest;
            boundViewModel = null;
        }
    }

    private void BindToViewModel()
    {
        UnBindFromViewModel();
        if (DataContext is QuestChainDocumentViewModel vm)
        {
            boundViewModel = vm;
            vm.NavigateToQuest += NavigateToQuest;
        }
    }

    private CancellationTokenSource? lastNavigateToken;

    private void NavigateToQuest(BaseQuestViewModel quest)
    {
        BaseQuestViewModel questToCompareTo = quest;
        if (quest is QuestViewModel q && q.ExclusiveGroup != null)
            questToCompareTo = q.ExclusiveGroup;

        async Task NavigateToQuestAsync(CancellationToken token)
        {
            await Task.Delay(100, token);

            token.ThrowIfCancellationRequested();

            while (boundViewModel?.IsCalculatingGraphLayout ?? false)
                await Task.Delay(16, token);

            token.ThrowIfCancellationRequested();

            await Task.Delay(100, token);

            token.ThrowIfCancellationRequested();

            do
            {
                Editor.BringIntoView(quest.Bounds.Center);
                var diff = (questToCompareTo.Location - new Point(questToCompareTo.PerfectX, questToCompareTo.PerfectY));
                var lengthSq = diff.X * diff.X + diff.Y * diff.Y;
                if (lengthSq < 2)
                    break;
                await Task.Delay(16, token);

                token.ThrowIfCancellationRequested();
            } while (true);
        }

        lastNavigateToken?.Cancel();
        lastNavigateToken = new();
        NavigateToQuestAsync(lastNavigateToken.Token).ListenWarnings();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        BindToViewModel();
    }

    private bool RelayoutGraph()
    {
        if (DataContext is QuestChainDocumentViewModel vm)
        {
            try
            {
                vm.Update();
            }
            catch (Exception e)
            {
                LOG.LogWarning(e);
            }
        }
        return true;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        timer?.Dispose();
        timer = null;
        UnBindFromViewModel();
    }

    private void DoScreenshot(object? sender, RoutedEventArgs e)
    {
        var target = this;
        Editor.FitToScreen();
        var scaling = 2;
        var pixelSize = new PixelSize((int) (target.Bounds.Width / Editor.ViewportZoom) * scaling, (int) (target.Bounds.Height / Editor.ViewportZoom) * scaling);
        var size = new Size(target.Bounds.Width / Editor.ViewportZoom, target.Bounds.Height / Editor.ViewportZoom);
        using RenderTargetBitmap bitmap = new RenderTargetBitmap(pixelSize, new Vector(96 / Editor.ViewportZoom * scaling, 96 / Editor.ViewportZoom * scaling));
        target.Measure(size);
        target.Arrange(new Rect(size));
        bitmap.Render(target);
        bitmap.Save("test.png");
    }

    private void FitToScreen(object? sender, RoutedEventArgs e)
    {
        Editor.FitToScreen();
    }

    private class TeleDestination
    {
        public SpawnKey SpawnKey { get; }
        public int MapId { get; }
        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float O { get; }
        public QuestRelationObjectType Type { get; }

        public TeleDestination(ICreature creature)
        {
            SpawnKey = creature.Key();
            MapId = creature.Map;
            X = creature.X;
            Y = creature.Y;
            Z = creature.Z;
            O = creature.O;
            Type = QuestRelationObjectType.Creature;
        }

        public TeleDestination(IGameObject go)
        {
            SpawnKey = go.Key();
            MapId = go.Map;
            X = go.X;
            Y = go.Y;
            Z = go.Z;
            O = 0;
            Type = QuestRelationObjectType.GameObject;
        }
    }

    private async Task LoadQuestStarters(MenuItem parentMenu, CancellationToken token)
    {
        await LoadQuestStartersEnders(parentMenu, true, token);
    }

    private async Task LoadQuestEnders(MenuItem parentMenu, CancellationToken token)
    {
        await LoadQuestStartersEnders(parentMenu, false, token);
    }

    private async Task LoadQuestStartersEnders(MenuItem parentMenu, bool starters, CancellationToken token)
    {
        if (DataContext is not QuestChainDocumentViewModel vm)
            return;

        var quest = parentMenu.CommandParameter as QuestViewModel;

        if (quest == null)
            return;

        var relations = await (starters ?
            vm.DatabaseProvider.GetQuestStarters(quest.Entry)
            : vm.DatabaseProvider.GetQuestEnders(quest.Entry));

        if (token.IsCancellationRequested)
            return;

        if (relations.Count == 0)
        {
            parentMenu.Items.Clear();
            parentMenu.Items.Add(new MenuItem() { Header = "No starters found" });
            return;
        }

        List<(string, QuestRelationObjectType, List<TeleDestination>)> teleports = new();
        foreach (var rel in relations)
        {
            var name = rel.Type == QuestRelationObjectType.Creature
                ? vm.DatabaseProvider.GetCachedCreatureTemplate(rel.Entry)?.Name
                : vm.DatabaseProvider.GetCachedGameObjectTemplate(rel.Entry)?.Name;

            List<TeleDestination> destinations = new();

            if (rel.Type == QuestRelationObjectType.Creature)
            {
                var creatureSpawns = await vm.DatabaseProvider.GetCreaturesByEntryAsync(rel.Entry);
                destinations.AddRange(creatureSpawns.Select(x => new TeleDestination(x)));
            }
            else if (rel.Type == QuestRelationObjectType.GameObject)
            {
                var goSpawns = await vm.DatabaseProvider.GetGameObjectsByEntryAsync(rel.Entry);
                destinations.AddRange(goSpawns.Select(x => new TeleDestination(x)));
            }
            else
                throw new ArgumentOutOfRangeException(nameof(rel.Type));

            if (destinations.Count == 0)
                continue;

            if (token.IsCancellationRequested)
                return;

            teleports.Add(($"{name} ({rel.Entry})", rel.Type, destinations));
        }

        if (token.IsCancellationRequested)
            return;

        parentMenu.Items.Clear();
        foreach (var (name, type, guids) in teleports)
        {
            if (guids.Count == 1)
            {
                parentMenu.Items.Add(new MenuItem()
                {
                    Header = name,
                    CommandParameter = guids[0],
                    Command = TeleportCommand
                });
            }
            else
            {
                var menuItem = new MenuItem()
                {
                    Header = name
                };
                foreach (var dest in guids)
                    menuItem.Items.Add(new MenuItem()
                    {
                        Header = $"Guid " + dest.SpawnKey.Guid,
                        Command = TeleportCommand,
                        CommandParameter = dest
                    });
                parentMenu.Items.Add(menuItem);
            }
        }
    }

    private void NodeContextMenuClosed(object? sender, RoutedEventArgs e)
    {
        teleportDestinationCancellation?.Cancel();
        TeleportToQuestStarterMenu.Items.Clear();
        TeleportToQuestStarterMenu.Items.Add(new MenuItem() { Header = "(loading)" });
        TeleportToQuestEnderMenu.Items.Clear();
        TeleportToQuestEnderMenu.Items.Add(new MenuItem() { Header = "(loading)" });
    }

    private void LoadQuest_MenuItemClick(object? sender, RoutedEventArgs e)
    {
        // need to run this on next frame, to let the context menu close
        // (otherwise LightDismissOverlay of pick menu will be closed too early by this context menu)
        DispatcherTimer.RunOnce(() =>
        {
            if (DataContext is QuestChainDocumentViewModel vm)
            {
                vm.LoadNewQuest(Editor.MouseLocation).ListenErrors();
            }
        }, TimeSpan.FromMilliseconds(1));
    }

    private void SearchTextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        bool found = false;
        var searchText = SearchTextBox.Text;
        if (!string.IsNullOrEmpty(searchText))
        {
            if (DataContext is QuestChainDocumentViewModel vm)
            {
                foreach (var node in vm.Elements)
                {
                    if (node is QuestViewModel qvm && (qvm.Entry.Contains(searchText) || qvm.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)))
                    {
                        Editor.UnselectAll();
                        vm.SelectedItems.Add(node);
                        Editor.BringIntoView(node.Bounds.Center);
                        found = true;
                        break;
                    }
                }
            }
        }
        NoResultsTextBlock.IsVisible = !found;
    }

    private void SearchTextBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (DataContext is QuestChainDocumentViewModel vm)
                vm.IsSearchBoxVisible = false;
            e.Handled = true;
        }
    }
}