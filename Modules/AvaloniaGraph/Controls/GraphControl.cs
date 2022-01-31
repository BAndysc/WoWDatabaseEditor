using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.Templates;

namespace AvaloniaGraph.Controls;

public class GraphControl : TemplatedControl
{
    private ConnectionsContainer connectionsContainer = null!;
    private NodesContainer nodesContainer = null!;

    public GraphControl()
    {
        AddHandler(ConnectorItem.ConnectorDragStartedEvent,
            new ConnectorItemDragStartedEventHandler(OnConnectorItemDragStarted));
        AddHandler(ConnectorItem.ConnectorDraggingEvent,
            new ConnectorItemDraggingEventHandler(OnConnectorItemDragging));
        AddHandler(ConnectorItem.ConnectorDragCompletedEvent,
            new ConnectorItemDragCompletedEventHandler(OnConnectorItemDragCompleted));
    }

    public IList SelectedElements => nodesContainer?.SelectedItems ?? new List<object>();

    public IEnumerable ElementsView => nodesContainer?.Items ?? Enumerable.Empty<object>();
    
    public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        nodesContainer = (NodesContainer)e.NameScope.Find("PART_ElementItemsControl");
        nodesContainer.SelectionChanged += OnNodesContainerSelectChanged;

        connectionsContainer = (ConnectionsContainer)e.NameScope.Find("PART_ConnectionItemsControl");
        connectionsContainer.SelectionChanged += OnConnectionsContainerSelectChanged;

        base.OnApplyTemplate(e);
    }

    private void OnConnectionsContainerSelectChanged(object? sender, SelectionChangedEventArgs e)
    {
        //if (e.AddedItems.Count > 0)
        //    elementItemsControl.SelectedItem = null;
    }

    private void OnNodesContainerSelectChanged(object? sender, SelectionChangedEventArgs e)
    {
        //if (e.AddedItems.Count > 0)
        //    connectionItemsControl.SelectedItem = null;
        if (SelectionChanged != null)
            SelectionChanged(this, new SelectionChangedEventArgs(e.RoutedEvent, e.RemovedItems, e.AddedItems));
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        nodesContainer!.SelectedItems.Clear();
        connectionsContainer!.SelectedItems.Clear();
        base.OnPointerPressed(e);
    }

    public int GetMaxZIndex()
    {
        return nodesContainer!.ItemContainerGenerator.Containers
            .Select(elementItem => ((GraphNodeItemView)elementItem.ContainerControl).ZIndex)
            .Concat(new[] { 0 })
            .Max();
    }

    #region Dependency properties

    public static readonly StyledProperty<IEnumerable> ElementsSourceProperty =
        AvaloniaProperty.Register<GraphControl, IEnumerable>(nameof(ElementsSource));

    public IEnumerable ElementsSource
    {
        get => GetValue(ElementsSourceProperty);
        set => SetValue(ElementsSourceProperty, value);
    }

    public static readonly StyledProperty<IDataTemplate> ElementItemTemplateProperty =
        AvaloniaProperty.Register<GraphControl, IDataTemplate>(nameof(ElementItemTemplate));

    public IDataTemplate ElementItemTemplate
    {
        get => GetValue(ElementItemTemplateProperty);
        set => SetValue(ElementItemTemplateProperty, value);
    }

    public static readonly StyledProperty<IEnumerable> ConnectionsSourceProperty =
        AvaloniaProperty.Register<GraphControl, IEnumerable>(nameof(ConnectionsSource));

    public IEnumerable ConnectionsSource
    {
        get => GetValue(ConnectionsSourceProperty);
        set => SetValue(ConnectionsSourceProperty, value);
    }
    
    public static readonly StyledProperty<IList> SelectedConnectionsProperty =
        AvaloniaProperty.Register<GraphControl, IList>(nameof(SelectedConnections));

    public IList SelectedConnections
    {
        get => GetValue(SelectedConnectionsProperty);
        set => SetValue(SelectedConnectionsProperty, value);
    }

    public static readonly StyledProperty<IDataTemplate> ConnectionItemTemplateProperty =
        AvaloniaProperty.Register<GraphControl, IDataTemplate>(
            nameof(ConnectionItemTemplate));

    public DataTemplate ConnectionItemTemplate
    {
        get => (DataTemplate)GetValue(ConnectionItemTemplateProperty);
        set => SetValue(ConnectionItemTemplateProperty, value);
    }

    #endregion

    #region Routed events

    public static readonly RoutedEvent<ConnectionDragStartedEventArgs> ConnectionDragStartedEvent =
        RoutedEvent.Register<GraphControl, ConnectionDragStartedEventArgs>("ConnectionDragStarted",
            RoutingStrategies.Bubble);

    public static readonly RoutedEvent<ConnectionDraggingEventArgs> ConnectionDraggingEvent =
        RoutedEvent.Register<GraphControl, ConnectionDraggingEventArgs>("ConnectionDragging",
            RoutingStrategies.Bubble);

    public static readonly RoutedEvent<ConnectionDragCompletedEventArgs> ConnectionDragCompletedEvent =
        RoutedEvent.Register<GraphControl, ConnectionDragCompletedEventArgs>("ConnectionDragCompleted",
            RoutingStrategies.Bubble);

    public event ConnectionDragStartedEventHandler ConnectionDragStarted
    {
        add => AddHandler(ConnectionDragStartedEvent, value);
        remove => RemoveHandler(ConnectionDragStartedEvent, value);
    }

    public event ConnectionDraggingEventHandler ConnectionDragging
    {
        add => AddHandler(ConnectionDraggingEvent, value);
        remove => RemoveHandler(ConnectionDraggingEvent, value);
    }

    public event ConnectionDragCompletedEventHandler ConnectionDragCompleted
    {
        add => AddHandler(ConnectionDragCompletedEvent, value);
        remove => RemoveHandler(ConnectionDragCompletedEvent, value);
    }

    #endregion

    #region Connection dragging

    private ConnectorItem? draggingSourceConnector;
    private object? draggingConnectionDataContext;

    private void OnConnectorItemDragStarted(object sender, ConnectorItemDragStartedEventArgs e)
    {
        e.Handled = true;
        draggingSourceConnector = e.SourceConnector;

        var eventArgs = new ConnectionDragStartedEventArgs(ConnectionDragStartedEvent,
            this,
            draggingSourceConnector.ParentElementItem,
            draggingSourceConnector,
            e.BaseArgs);

        RaiseEvent(eventArgs);

        draggingConnectionDataContext = eventArgs.Connection;

        if (draggingConnectionDataContext == null)
            e.Cancel = true;
    }

    private void OnConnectorItemDragging(object sender, ConnectorItemDraggingEventArgs e)
    {
        if (draggingSourceConnector == null || draggingConnectionDataContext == null)
            return;

        e.Handled = true;

        var connectionDraggingEventArgs = new ConnectionDraggingEventArgs(ConnectionDraggingEvent,
            this,
            draggingSourceConnector.ParentElementItem,
            draggingConnectionDataContext,
            draggingSourceConnector,
            e.BaseArgs);
        RaiseEvent(connectionDraggingEventArgs);
    }

    private void OnConnectorItemDragCompleted(object sender, ConnectorItemDragCompletedEventArgs e)
    {
        if (draggingSourceConnector == null || draggingConnectionDataContext == null)
            return;

        e.Handled = true;

        RaiseEvent(new ConnectionDragCompletedEventArgs(ConnectionDragCompletedEvent,
            this,
            draggingSourceConnector.ParentElementItem,
            draggingConnectionDataContext,
            draggingSourceConnector,
            e.BaseArgs));

        draggingSourceConnector = null;
        draggingConnectionDataContext = null;
    }

    #endregion

    public void ClearSelection()
    {
        SelectedConnections.Clear();
        SelectedElements.Clear();
    }
}