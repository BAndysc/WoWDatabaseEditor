using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using WDE.MVVM;
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
                if (DirectEditParameter != null)
                {
                    /*if (e.OriginalSource is Run originalRun && originalRun.DataContext != null &&
                        originalRun.DataContext != DataContext)
                    {
                        DirectEditParameter.Execute(originalRun.DataContext);
                        return;
                    }*/
                }

                DeselectAllButActionsRequest?.Execute(null);
               
                if (!IsSelected)
                {
                    if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
                        DeselectAllRequest?.Execute(null);
                    IsSelected = true;
                }
            }
            else if (e.ClickCount == 2)
                EditActionCommand?.Execute(DataContext);

            e.Handled = true;
        }

        protected override void OnDataContextEndUpdate()
        {
            var isComment = DataContext is SmartAction action && action.Id == SmartConstants.ActionComment;
            PseudoClasses.Set(":comment", isComment);
            PseudoClasses.Set(":action", !isComment);
        }
    }
}