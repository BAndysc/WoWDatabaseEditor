using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace GeminiGraphEditor
{
    // Inspired by studying http://www.codeproject.com/Articles/182683/NetworkView-A-WPF-custom-control-for-visualizing-a
    // Thank you Ashley Davis!
    public class GraphControl : Control
    {
        private ConnectionItemsControl connectionItemsControl;
        private ElementItemsControl elementItemsControl;

        static GraphControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(GraphControl), new FrameworkPropertyMetadata(typeof(GraphControl)));
        }

        public GraphControl()
        {
            AddHandler(ConnectorItem.ConnectorDragStartedEvent, new ConnectorItemDragStartedEventHandler(OnConnectorItemDragStarted));
            AddHandler(ConnectorItem.ConnectorDraggingEvent, new ConnectorItemDraggingEventHandler(OnConnectorItemDragging));
            AddHandler(ConnectorItem.ConnectorDragCompletedEvent,
                new ConnectorItemDragCompletedEventHandler(OnConnectorItemDragCompleted));
        }

        public IList SelectedElements => elementItemsControl.SelectedItems;

        public IList ElementsView => elementItemsControl.Items;

        public IList SelectedConnections => connectionItemsControl.SelectedItems;

        public event SelectionChangedEventHandler SelectionChanged;

        public override void OnApplyTemplate()
        {
            elementItemsControl = (ElementItemsControl) Template.FindName("PART_ElementItemsControl", this);
            elementItemsControl.SelectionChanged += OnElementItemsControlSelectChanged;

            connectionItemsControl = (ConnectionItemsControl) Template.FindName("PART_ConnectionItemsControl", this);
            //_connectionItemsControl.SelectionChanged += OnConnectionItemsControlSelectChanged;

            base.OnApplyTemplate();
        }

        private void OnElementItemsControlSelectChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectionChangedEventHandler handler = SelectionChanged;
            if (handler != null)
                handler(this, new SelectionChangedEventArgs(Selector.SelectionChangedEvent, e.RemovedItems, e.AddedItems));
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            elementItemsControl.SelectedItems.Clear();
            connectionItemsControl.SelectedItems.Clear();
            base.OnMouseLeftButtonDown(e);
        }

        public int GetMaxZIndex()
        {
            return elementItemsControl.Items.Cast<object>()
                .Select(item => (ElementItem) elementItemsControl.ItemContainerGenerator.ContainerFromItem(item))
                .Select(elementItem => elementItem.ZIndex)
                .Concat(new[] {0})
                .Max();
        }

        #region Dependency properties

        public static readonly DependencyProperty ElementsSourceProperty =
            DependencyProperty.Register("ElementsSource", typeof(IEnumerable), typeof(GraphControl));

        public IEnumerable ElementsSource
        {
            get => (IEnumerable) GetValue(ElementsSourceProperty);
            set => SetValue(ElementsSourceProperty, value);
        }

        public static readonly DependencyProperty ElementItemContainerStyleProperty =
            DependencyProperty.Register("ElementItemContainerStyle", typeof(Style), typeof(GraphControl));

        public Style ElementItemContainerStyle
        {
            get => (Style) GetValue(ElementItemContainerStyleProperty);
            set => SetValue(ElementItemContainerStyleProperty, value);
        }

        public static readonly DependencyProperty ElementItemTemplateProperty =
            DependencyProperty.Register("ElementItemTemplate", typeof(DataTemplate), typeof(GraphControl));

        public DataTemplate ElementItemTemplate
        {
            get => (DataTemplate) GetValue(ElementItemTemplateProperty);
            set => SetValue(ElementItemTemplateProperty, value);
        }

        public static readonly DependencyProperty ElementItemDataTemplateSelectorProperty = DependencyProperty.Register(
            "ElementItemDataTemplateSelector",
            typeof(DataTemplateSelector),
            typeof(GraphControl),
            new PropertyMetadata(null));

        public DataTemplateSelector ElementItemDataTemplateSelector
        {
            get => (DataTemplateSelector) GetValue(ElementItemDataTemplateSelectorProperty);
            set => SetValue(ElementItemDataTemplateSelectorProperty, value);
        }

        public static readonly DependencyProperty ConnectionItemDataTemplateSelectorProperty = DependencyProperty.Register(
            "ConnectionItemDataTemplateSelector",
            typeof(DataTemplateSelector),
            typeof(GraphControl),
            new PropertyMetadata(null));

        public DataTemplateSelector ConnectionItemDataTemplateSelector
        {
            get => (DataTemplateSelector) GetValue(ConnectionItemDataTemplateSelectorProperty);
            set => SetValue(ConnectionItemDataTemplateSelectorProperty, value);
        }

        public static readonly DependencyProperty ConnectionsSourceProperty =
            DependencyProperty.Register("ConnectionsSource", typeof(IEnumerable), typeof(GraphControl));

        public IEnumerable ConnectionsSource
        {
            get => (IEnumerable) GetValue(ConnectionsSourceProperty);
            set => SetValue(ConnectionsSourceProperty, value);
        }

        public static readonly DependencyProperty ConnectionItemContainerStyleProperty =
            DependencyProperty.Register("ConnectionItemContainerStyle", typeof(Style), typeof(GraphControl));

        public Style ConnectionItemContainerStyle
        {
            get => (Style) GetValue(ConnectionItemContainerStyleProperty);
            set => SetValue(ConnectionItemContainerStyleProperty, value);
        }

        public static readonly DependencyProperty ConnectionItemTemplateProperty = DependencyProperty.Register(
            "ConnectionItemTemplate",
            typeof(DataTemplate),
            typeof(GraphControl));

        public DataTemplate ConnectionItemTemplate
        {
            get => (DataTemplate) GetValue(ConnectionItemTemplateProperty);
            set => SetValue(ConnectionItemTemplateProperty, value);
        }

        #endregion

        #region Routed events

        public static readonly RoutedEvent ConnectionDragStartedEvent = EventManager.RegisterRoutedEvent("ConnectionDragStarted",
            RoutingStrategy.Bubble,
            typeof(ConnectionDragStartedEventHandler),
            typeof(GraphControl));

        public static readonly RoutedEvent ConnectionDraggingEvent = EventManager.RegisterRoutedEvent("ConnectionDragging",
            RoutingStrategy.Bubble,
            typeof(ConnectionDraggingEventHandler),
            typeof(GraphControl));

        public static readonly RoutedEvent ConnectionDragCompletedEvent = EventManager.RegisterRoutedEvent("ConnectionDragCompleted",
            RoutingStrategy.Bubble,
            typeof(ConnectionDragCompletedEventHandler),
            typeof(GraphControl));

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

        private ConnectorItem draggingSourceConnector;
        private object draggingConnectionDataContext;

        private void OnConnectorItemDragStarted(object sender, ConnectorItemDragStartedEventArgs e)
        {
            e.Handled = true;

            draggingSourceConnector = (ConnectorItem) e.OriginalSource;

            ConnectionDragStartedEventArgs eventArgs = new ConnectionDragStartedEventArgs(ConnectionDragStartedEvent,
                this,
                draggingSourceConnector.ParentElementItem,
                draggingSourceConnector);
            RaiseEvent(eventArgs);

            draggingConnectionDataContext = eventArgs.Connection;

            if (draggingConnectionDataContext == null)
                e.Cancel = true;
        }

        private void OnConnectorItemDragging(object sender, ConnectorItemDraggingEventArgs e)
        {
            e.Handled = true;

            ConnectionDraggingEventArgs connectionDraggingEventArgs = new ConnectionDraggingEventArgs(ConnectionDraggingEvent,
                this,
                draggingSourceConnector.ParentElementItem,
                draggingConnectionDataContext,
                draggingSourceConnector);
            RaiseEvent(connectionDraggingEventArgs);
        }

        private void OnConnectorItemDragCompleted(object sender, ConnectorItemDragCompletedEventArgs e)
        {
            e.Handled = true;

            RaiseEvent(new ConnectionDragCompletedEventArgs(ConnectionDragCompletedEvent,
                this,
                draggingSourceConnector.ParentElementItem,
                draggingConnectionDataContext,
                draggingSourceConnector));

            draggingSourceConnector = null;
            draggingConnectionDataContext = null;
        }

        #endregion
    }
}