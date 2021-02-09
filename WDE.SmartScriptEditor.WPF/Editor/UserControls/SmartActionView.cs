using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.WPF.Editor.UserControls
{
    /// <summary>
    ///     Interaction logic for SmartActionView.xaml
    /// </summary>
    public class SmartActionView : Control
    {
        public static DependencyProperty IsSelectedProperty = DependencyProperty.Register(nameof(IsSelected),
            typeof(bool),
            typeof(SmartActionView),
            new PropertyMetadata(false));

        public static DependencyProperty DeselectAllRequestProperty =
            DependencyProperty.Register(nameof(DeselectAllRequest), typeof(ICommand), typeof(SmartActionView));

        public static DependencyProperty DeselectAllButActionsRequestProperty =
            DependencyProperty.Register(nameof(DeselectAllButActionsRequest), typeof(ICommand), typeof(SmartActionView));

        public static DependencyProperty EditActionCommandProperty =
            DependencyProperty.Register(nameof(EditActionCommand), typeof(ICommand), typeof(SmartActionView));
        
        public static DependencyProperty DirectEditParameterProperty =
            DependencyProperty.Register(nameof(DirectEditParameter), typeof(ICommand), typeof(SmartActionView));
        
        static SmartActionView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SmartActionView), new FrameworkPropertyMetadata(typeof(SmartActionView)));
        }
        
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

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.ClickCount == 1)
            {
                if (DirectEditParameter != null)
                {
                    if (e.OriginalSource is Run originalRun && originalRun.DataContext != null &&
                        originalRun.DataContext != DataContext)
                    {
                        DirectEditParameter.Execute(originalRun.DataContext);
                        return;
                    }
                }

                DeselectAllButActionsRequest?.Execute(null);
               
                if (!IsSelected)
                {
                    if (!Keyboard.IsKeyDown(Key.LeftCtrl))
                        DeselectAllRequest?.Execute(null);
                    IsSelected = true;
                }
            }
            else if (e.ClickCount == 2)
                EditActionCommand?.Execute(DataContext);
        }
    }
}