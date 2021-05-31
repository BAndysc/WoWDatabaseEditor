using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using WDE.Common.Avalonia.Controls;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Avalonia.Editor.UserControls
{
    /// <summary>
    ///     Interaction logic for SmartActionView.xaml
    /// </summary>
    public class SmartActionView : SelectableTemplatedControl
    {
        public static AvaloniaProperty DeselectAllRequestProperty =
            AvaloniaProperty.Register<SmartActionView, ICommand>(nameof(DeselectAllRequest));

        public static AvaloniaProperty DeselectAllButActionsRequestProperty =
            AvaloniaProperty.Register<SmartActionView, ICommand>(nameof(DeselectAllButActionsRequest));

        public static AvaloniaProperty EditActionCommandProperty =
            AvaloniaProperty.Register<SmartActionView, ICommand>(nameof(EditActionCommand));
        
        public static AvaloniaProperty DirectEditParameterProperty =
            AvaloniaProperty.Register<SmartActionView, ICommand>(nameof(DirectEditParameter));
        
        public ICommand DeselectAllRequest
        {
            get => (ICommand) GetValue(DeselectAllRequestProperty);
            set => SetValue(DeselectAllRequestProperty, value);
        }

        public ICommand DeselectAllButActionsRequest
        {
            get => (ICommand) GetValue(DeselectAllButActionsRequestProperty);
            set => SetValue(DeselectAllButActionsRequestProperty, value);
        }

        public ICommand EditActionCommand
        {
            get => (ICommand) GetValue(EditActionCommandProperty);
            set => SetValue(EditActionCommandProperty, value);
        }
        
        public ICommand DirectEditParameter
        {
            get => (ICommand) GetValue(DirectEditParameterProperty);
            set => SetValue(DirectEditParameterProperty, value);
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (e.ClickCount == 1)
            {
                if (e.Source is FormattedTextBlock tb && tb.OverContext != null)
                {
                    return;
                }

                DeselectAllButActionsRequest?.Execute(null);
               
                if (!IsSelected)
                {
                    if (!e.KeyModifiers.HasFlag(MultiselectGesture))
                        DeselectAllRequest?.Execute(null);
                    IsSelected = true;
                }
                e.Handled = true;
            }
        }

        protected override void OnDirectEdit(object context)
        {
            DirectEditParameter?.Execute(context);
        }

        protected override void OnEdit()
        {
            EditActionCommand?.Execute(DataContext);
        }

        protected override void OnDataContextEndUpdate()
        {
            var isComment = DataContext is SmartAction action && action.Id == SmartConstants.ActionComment;
            PseudoClasses.Set(":comment", isComment);
            PseudoClasses.Set(":action", !isComment);
        }
    }
}