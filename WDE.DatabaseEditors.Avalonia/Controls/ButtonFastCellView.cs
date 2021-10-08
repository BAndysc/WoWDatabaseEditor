using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace WDE.DatabaseEditors.Avalonia.Controls
{
    public class ButtonFastCellView : FastCellViewBase
    {
        private Button? partButton;
        
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            partButton = e.NameScope.Find<Button>("PART_Button");
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            HandleMoveLeftRightUpBottom(e, true);
            if (e.Key == Key.Space || e.Key == Key.Enter)
            {
                partButton?.Command?.Execute(partButton.CommandParameter);
            }
        }
    }
}