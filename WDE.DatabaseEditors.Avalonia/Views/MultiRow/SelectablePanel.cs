using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace WDE.DatabaseEditors.Avalonia.Views.MultiRow
{
    public class SelectablePanel : Panel
    {
        public static readonly StyledProperty<bool> IsSelectedProperty = AvaloniaProperty.Register<SelectablePanel, bool>(nameof(IsSelected));
        public static readonly AttachedProperty<object?> SelectedItemProperty = AvaloniaProperty.RegisterAttached<AvaloniaObject, object?>("SelectedItem", typeof(SelectablePanel));
        public static readonly AttachedProperty<bool> ObserveItemsProperty = AvaloniaProperty.RegisterAttached<AvaloniaObject, bool>("ObserveItems", typeof(SelectablePanel));

        static SelectablePanel()
        {
            PointerPressedEvent.AddClassHandler<SelectablePanel>(HandlePointerPressed, RoutingStrategies.Tunnel);
            IsSelectedProperty.Changed.AddClassHandler<SelectablePanel>(IsSelectedChanged);
            SelectedItemProperty.Changed.AddClassHandler<ItemsControl>(SelectedItemChanged);
            ObserveItemsProperty.Changed.AddClassHandler<ItemsControl>(ObserveItemsChanged);
        }

        private static void IsSelectedChanged(SelectablePanel arg1, AvaloniaPropertyChangedEventArgs arg2)
        {
            arg1.PseudoClasses.Set(":selected", arg2.NewValue is true);
        }

        private static void ObserveItemsChanged(ItemsControl arg1, AvaloniaPropertyChangedEventArgs arg2)
        {
            // @todo avalonia11
            //arg1.ItemContainerGenerator.Materialized += ItemContainerGeneratorOnMaterialized;
        }

        // @todo avalonia11
        // private static void ItemContainerGeneratorOnMaterialized(object? sender, ItemContainerEventArgs e)
        // {
        //     var icg = sender as ItemContainerGenerator;
        //     if (icg == null)
        //         return;
        //     var ic = icg.Owner as ItemsControl;
        //     if (ic == null)
        //         return;
        //     UpdateSelected(ic, GetSelectedItem(ic));
        // }

        private static void UpdateSelected(ItemsControl arg1, object? newValue)
        {
            for (int i = 0; i < arg1.ItemCount; ++i)
            {
                var panel = arg1.ItemContainerGenerator.ContainerFromIndex(i) as ContentPresenter;
                if (panel != null && panel.Child is SelectablePanel sp)
                    sp.IsSelected = newValue == panel.DataContext;
            }
        }
        
        private static void SelectedItemChanged(ItemsControl arg1, AvaloniaPropertyChangedEventArgs arg2)
        {
            UpdateSelected(arg1, arg2.NewValue);
        }

        public bool IsSelected
        {
            get => GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        public void Select()
        {
            var parent = this.FindAncestorOfType<ItemsControl>();
            if (parent != null)
                SetSelectedItem(parent, DataContext);
        }

        private static void HandlePointerPressed(SelectablePanel panel, PointerPressedEventArgs args)
        {
            panel.Select();
        }

        public static object? GetSelectedItem(AvaloniaObject obj)
        {
            return (object?)obj.GetValue(SelectedItemProperty);
        }

        public static void SetSelectedItem(AvaloniaObject obj, object? value)
        {
            obj.SetValue(SelectedItemProperty, value);
        }

        public static bool GetObserveItems(AvaloniaObject obj)
        {
            return (bool?)obj.GetValue(ObserveItemsProperty) ?? false;
        }

        public static void SetObserveItems(AvaloniaObject obj, bool value)
        {
            obj.SetValue(ObserveItemsProperty, value);
        }
    }
}