using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WoWDatabaseEditor.Extensions
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
                control.Loaded += (sender, e) => control.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }
    }
}