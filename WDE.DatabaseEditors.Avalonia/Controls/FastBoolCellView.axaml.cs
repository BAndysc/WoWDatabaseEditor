using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace WDE.DatabaseEditors.Avalonia.Controls
{
    public class FastBoolCellView : FastCellViewBase
    {
        private CheckBox? checkBox;
        private Panel? panel;
        
        static FastBoolCellView()
        {
            PointerPressedEvent.AddClassHandler(typeof(FastBoolCellView), (sender, args) =>
            {
                if (sender is not FastBoolCellView that || args is not PointerPressedEventArgs e)
                    return;
                
                if (that.isReadOnly || e.Handled)
                    return;

                if (!e.GetCurrentPoint(that).Properties.IsLeftButtonPressed)
                    return;
            
                if (!ReferenceEquals(e.Source, that) && !ReferenceEquals(e.Source, that.panel))
                    return;
                
                if (!that.IsFocused)
                {
                    that.Focus(NavigationMethod.Tab);
                    e.Handled = true;
                }
            }, RoutingStrategies.Tunnel);
        }
        
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            panel = e.NameScope.Find<Panel>("PART_Panel");
            checkBox = e.NameScope.Find<CheckBox>("PART_CheckBox");
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            HandleMoveLeftRightUpBottom(e, true);
            if (e.Key == Key.Space || e.Key == Key.Enter)
            {
                if (checkBox != null)
                    checkBox.IsChecked = !checkBox.IsChecked;
                e.Handled = true;
            }
        }
    }
}
