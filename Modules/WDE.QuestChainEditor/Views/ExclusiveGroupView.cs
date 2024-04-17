using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Nodify;
using WDE.QuestChainEditor.ViewModels;

namespace WDE.QuestChainEditor.Views
{
    public class ExclusiveGroupView : ContentControl
    {
        protected const string ElementResizeThumb = "PART_ResizeThumb";
        protected const string ElementHeader = "PART_Header";
        protected const string ElementContent = "PART_Content";

        #region Routed Events

        public static readonly RoutedEvent<ResizeEventArgs> ResizeStartedEvent = RoutedEvent.Register<ResizeEventArgs>(nameof(ResizeStarted), RoutingStrategies.Bubble, typeof(ExclusiveGroupView));
        public static readonly RoutedEvent<ResizeEventArgs> ResizeCompletedEvent = RoutedEvent.Register<ResizeEventArgs>(nameof(ResizeCompleted), RoutingStrategies.Bubble, typeof(ExclusiveGroupView));

        /// <summary>
        /// Occurs when the node finished resizing.
        /// </summary>
        public event ResizeEventHandler ResizeCompleted
        {
            add => AddHandler(ResizeCompletedEvent, value);
            remove => RemoveHandler(ResizeCompletedEvent, value);
        }

        /// <summary>
        /// Occurs when the node started resizing.
        /// </summary>
        public event ResizeEventHandler ResizeStarted
        {
            add => AddHandler(ResizeStartedEvent, value);
            remove => RemoveHandler(ResizeStartedEvent, value);
        }

        #endregion

        #region Dependency Properties

        public static readonly StyledProperty<Size> ActualSizeProperty = AvaloniaProperty.Register<ExclusiveGroupView, Size>(nameof(ActualSize), BoxValue.Size, defaultBindingMode: BindingMode.TwoWay);

        private static void OnActualSizeChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            var node = (ExclusiveGroupView)d;
            var newSize = (Size)e.NewValue!;
            node.Width = newSize.Width;
            node.Height = newSize.Height;
        }

        /// <summary>
        /// Gets or sets the actual size of this <see cref="ExclusiveGroupView"/>.
        /// </summary>
        public Size ActualSize
        {
            get => (Size)GetValue(ActualSizeProperty);
            set => SetValue(ActualSizeProperty, value);
        }

        #endregion

        #region Fields

        /// <summary>
        /// Gets the <see cref="NodifyEditor"/> that owns this <see cref="ExclusiveGroupView"/>.
        /// </summary>
        protected NodifyEditor? Editor { get; private set; }

        /// <summary>
        /// Gets the <see cref="NodifyEditor"/> that owns this <see cref="Container"/>.
        /// </summary>
        protected ItemContainer? Container { get; private set; }

        private double _minHeight = 30;
        private double _minWidth = 30;

        #endregion

        static ExclusiveGroupView()
        {
            ActualSizeProperty.Changed.AddClassHandler<ExclusiveGroupView>(OnActualSizeChanged);
            ZIndexProperty.OverrideMetadata<ExclusiveGroupView>(new StyledPropertyMetadata<int>(-1));
            ZIndexProperty.Changed.AddClassHandler<ExclusiveGroupView>(OnZIndexPropertyChanged);
        }

        private static void OnZIndexPropertyChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            var node = (ExclusiveGroupView)d;
            if (node.Container != null)
            {
                node.Container.SetCurrentValue(ZIndexProperty, e.NewValue);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExclusiveGroupView"/> class.
        /// </summary>
        public ExclusiveGroupView()
        {
            // AddHandler(Thumb.DragDeltaEvent, OnResize);
            // AddHandler(Thumb.DragCompletedEvent, OnResizeCompleted);
            // AddHandler(Thumb.DragStartedEvent, OnResizeStarted);
            //
            // Loaded += OnNodeLoaded;
            // Unloaded += OnNodeUnloaded;
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            if (Editor == null)
                return;

            if (DataContext is not ExclusiveGroupViewModel group)
                return;

            foreach (var q in group.Quests)
            {
                q.IsSelected = true;
            }
        }

        // private void OnNodeLoaded(object? sender, RoutedEventArgs e)
        // {
        //     if (HeaderControl != null)
        //     {
        //         HeaderControl.PointerPressed += OnHeaderMouseDown;
        //         HeaderControl.SizeChanged += OnHeaderSizeChanged;
        //         CalculateDesiredHeaderSize();
        //     }
        // }
        //
        // private void OnNodeUnloaded(object? sender, RoutedEventArgs e)
        // {
        //     if (HeaderControl != null)
        //     {
        //         HeaderControl.PointerPressed -= OnHeaderMouseDown;
        //         HeaderControl.SizeChanged -= OnHeaderSizeChanged;
        //     }
        // }
        //
        // private void OnHeaderMouseDown(object? sender, PointerPressedEventArgs e)
        // {
        //     if (Container != null && Editor != null && EditorGestures.ItemContainer.Drag.Matches(e.Source, e))
        //     {
        //         // Switch the default movement mode if necessary
        //         var prevMovementMode = MovementMode;
        //         if (e.KeyModifiers == EditorGestures.GroupingNode.SwitchMovementMode)
        //         {
        //             SetCurrentValue(MovementModeProperty, MovementMode == GroupingMovementMode.Group ? GroupingMovementMode.Self : GroupingMovementMode.Group);
        //         }
        //
        //         // Select the content and move with it
        //         if (EditorGestures.Selection.Append.Matches(e.Source, e))
        //         {
        //             Editor.SelectArea(new Rect(Container.Location, Bounds.Size /* RenderSize */), append: true, fit: true);
        //         }
        //         else if (EditorGestures.Selection.Remove.Matches(e.Source, e))
        //         {
        //             Editor.UnselectArea(new Rect(Container.Location, Bounds.Size /* RenderSize */), fit: true);
        //         }
        //         else if (EditorGestures.Selection.Invert.Matches(e.Source, e))
        //         {
        //             if (Container.IsSelected)
        //             {
        //                 Editor.UnselectArea(new Rect(Container.Location, Bounds.Size /* RenderSize */), fit: true);
        //                 Container.IsSelected = true;
        //             }
        //             else
        //             {
        //                 Editor.SelectArea(new Rect(Container.Location, Bounds.Size /* RenderSize */), append: true, fit: true);
        //             }
        //         }
        //         else if (EditorGestures.Selection.Replace.Matches(e.Source, e) || EditorGestures.ItemContainer.Drag.Matches(e.Source, e))
        //         {
        //             Editor.SelectArea(new Rect(Container.Location, Bounds.Size /* RenderSize */), append: Container.IsSelected, fit: true);
        //         }
        //
        //         // Deselect content
        //         if (MovementMode == GroupingMovementMode.Self)
        //         {
        //             Editor.UnselectArea(new Rect(Container.Location, Bounds.Size /* RenderSize */), fit: true);
        //             Container.IsSelected = true;
        //         }
        //
        //         // Switch the default movement mode back
        //         SetCurrentValue(MovementModeProperty, prevMovementMode);
        //     }
        // }
        //
        // /// <inheritdoc />
        // protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        // {
        //     base.OnApplyTemplate(e);
        //
        //     ResizeThumb = e.NameScope.Find<Control>(ElementResizeThumb);
        //     HeaderControl = e.NameScope.Find<Control>(ElementHeader);
        //     ContentControl = e.NameScope.Find<Control>(ElementContent);
        //
        //     Container = this.FindAncestorOfType<ItemContainer>();
        //     Editor = Container?.Editor ?? this.FindAncestorOfType<NodifyEditor>();
        //
        //     if (Container != null)
        //     {
        //         Container.SetCurrentValue(ZIndexProperty, this.GetValue(ZIndexProperty));
        //     }
        // }
    }
}
