using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WoWDatabaseEditorCore.WPF.Extensions
{
    public static class FocusBehavior
    {
        public static readonly DependencyProperty FocusFirstProperty = DependencyProperty.RegisterAttached("FocusFirst",
            typeof(bool),
            typeof(FocusBehavior),
            new PropertyMetadata(false, OnFocusFirstPropertyChanged));

        public static bool GetFocusFirst(Control control)
        {
            return (bool) control.GetValue(FocusFirstProperty);
        }

        public static void SetFocusFirst(Control control, bool value)
        {
            control.SetValue(FocusFirstProperty, value);
        }

        private static void OnFocusFirstPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            Control? control = obj as Control;
            if (control == null || !(args.NewValue is bool))
                return;

            if ((bool) args.NewValue)
                control.Loaded += ControlOnLoaded;
        }

        private static void ControlOnLoaded(object sender, RoutedEventArgs e)
        {
            (sender as Control)!.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            (sender as Control)!.Loaded -= ControlOnLoaded;
        }
    }
}