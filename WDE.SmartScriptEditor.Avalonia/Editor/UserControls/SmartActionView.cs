using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using WDE.MVVM;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Avalonia.Editor.UserControls
{
    /// <summary>
    ///     Interaction logic for SmartActionView.xaml
    /// </summary>
    public class SmartActionView : TemplatedControl
    {
        public static AvaloniaProperty IsSelectedProperty = AvaloniaProperty.Register<SmartActionView, bool>(nameof(IsSelected));

        public static AvaloniaProperty DeselectAllRequestProperty =
            AvaloniaProperty.Register<SmartActionView, ICommand>(nameof(DeselectAllRequest));

        public static AvaloniaProperty DeselectAllButActionsRequestProperty =
            AvaloniaProperty.Register<SmartActionView, ICommand>(nameof(DeselectAllButActionsRequest));

        public static AvaloniaProperty EditActionCommandProperty =
            AvaloniaProperty.Register<SmartActionView, ICommand>(nameof(EditActionCommand));
        
        public static AvaloniaProperty DirectEditParameterProperty =
            AvaloniaProperty.Register<SmartActionView, ICommand>(nameof(DirectEditParameter));
        
        
        public bool IsSelected
        {
            get => (bool) GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

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
        
        private System.IDisposable sub;
        protected override void OnDataContextChanged(EventArgs e)
        {
            sub?.Dispose();
            base.OnDataContextChanged(e);

            if (DataContext == null)
                sub = null;
            else
                sub = (DataContext as SmartAction).ToObservable(e => e.IsSelected).Subscribe(@is =>
                {
                    if (@is)
                        PseudoClasses.Add(":selected");
                    else
                        PseudoClasses.Remove(":selected");
                });
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
        }
    }
}