using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace WDE.SmartScriptEditor.Editor.Views.Helpers
{
    public class MoveFocusOnArrowsTextBox : Behavior<TextBox>
    {
        protected override void OnAttached()
        {
            AssociatedObject.PreviewKeyDown += AssociatedObject_PreviewKeyDown;
        }

        private void AssociatedObject_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up || e.Key == Key.Down)
            {
                var request = new TraversalRequest(e.Key == Key.Down ? FocusNavigationDirection.Down : FocusNavigationDirection.Up) {Wrapped = false};
                AssociatedObject.MoveFocus(request);
            }
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewKeyDown -= AssociatedObject_PreviewKeyDown;
        }
    }
    
    public class MoveFocusOnArrowsCheckBox : Behavior<CheckBox>
    {
        protected override void OnAttached()
        {
            AssociatedObject.PreviewKeyDown += AssociatedObject_PreviewKeyDown;
        }

        private void AssociatedObject_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up || e.Key == Key.Down)
            {
                var request = new TraversalRequest(e.Key == Key.Down ? FocusNavigationDirection.Down : FocusNavigationDirection.Up) {Wrapped = false};
                AssociatedObject.MoveFocus(request);
            }
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewKeyDown -= AssociatedObject_PreviewKeyDown;
        }
    }
}