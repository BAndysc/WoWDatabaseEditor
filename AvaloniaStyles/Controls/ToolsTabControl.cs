using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Styling;

namespace AvaloniaStyles.Controls
{
    public class ToolsTabControl : TabControl, IStyleable
    {
        public static readonly AvaloniaProperty AttachedViewProperty =
            AvaloniaProperty.RegisterAttached<ToolsTabControl, TabItem, IControl>("AttachedView");

        public static IControl GetAttachedView(TabItem control) => (IControl) control.GetValue(AttachedViewProperty);

        public static void SetAttachedView(TabItem control, IControl value) =>
            control.SetValue(AttachedViewProperty, value);

        public static readonly StyledProperty<IControl> ActiveViewProperty =
            AvaloniaProperty.Register<ToolsTabControl, IControl>(nameof(ActiveView));

        public IControl ActiveView
        {
            get => GetValue(ActiveViewProperty);
            internal set => SetValue(ActiveViewProperty, value);
        }

        internal IContentPresenter ContentPart { get; private set; }
        private static readonly FuncTemplate<IPanel> DefaultPanel =
            new FuncTemplate<IPanel>(() => new StackPanel(){Name = "ToolsList"});
        
        static ToolsTabControl()
        {
            ItemsPanelProperty.OverrideDefaultValue<ToolsTabControl>(DefaultPanel);
            
            SelectedContentProperty.Changed.AddClassHandler<ToolsTabControl>((viewModelItemsControl, e) =>
            {
                if (viewModelItemsControl.SelectedItem != null &&
                    viewModelItemsControl.itemsToViews.TryGetValue(viewModelItemsControl.SelectedItem, out var view))
                    viewModelItemsControl.ActiveView = view;
                else
                    viewModelItemsControl.ActiveView = null;
                if (viewModelItemsControl.ContentPart != null)
                    viewModelItemsControl.ContentPart.DataContext = viewModelItemsControl.SelectedItem;
            });
        }

        protected override bool RegisterContentPresenter(IContentPresenter presenter)
        {
            base.RegisterContentPresenter(presenter);
            if (presenter.Name == "PART_SelectedContentHost")
            {
                ContentPart = presenter;
                return true;
            }

            return false;
        }

        protected override void OnContainersMaterialized(ItemContainerEventArgs e)
        {
            base.OnContainersMaterialized(e);

            foreach (var container in e.Containers)
            {
                if (container.Item != null && itemsToViews.TryGetValue(container.Item, out var vieww))
                    SetAttachedView(container.ContainerControl as TabItem, vieww as IControl);
            }
        }

        private Dictionary<object, IControl> itemsToViews = new();

        protected override void ItemsChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.NewValue != null)
                GenerateContent(e.NewValue as IList);
            base.ItemsChanged(e);
        }

        protected override void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    GenerateContent(e.NewItems);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    FreeContent(e.OldItems);
                    break;
            }

            base.ItemsCollectionChanged(sender, e);
        }

        private void FreeContent(IList eOldItems)
        {
            foreach (var o in eOldItems)
                itemsToViews.Remove(o);
        }

        private void GenerateContent(IList eNewItems)
        {
            foreach (var o in eNewItems)
            {
                IControl view = GenerateView(o);
                itemsToViews[o] = view;
            }
        }

        protected virtual IControl GenerateView(object viewModel)
        {
            return new TextBlock() {Text = viewModel.ToString()};
        }
    }
}