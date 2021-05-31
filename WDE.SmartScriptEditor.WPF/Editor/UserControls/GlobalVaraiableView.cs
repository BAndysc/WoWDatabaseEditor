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
    public class GlobalVariableView : Control
    {
        public static DependencyProperty IsSelectedProperty = DependencyProperty.Register(nameof(IsSelected),
            typeof(bool),
            typeof(GlobalVariableView),
            new PropertyMetadata(false));

        public static DependencyProperty DeselectAllRequestProperty =
            DependencyProperty.Register(nameof(DeselectAllRequest), typeof(ICommand), typeof(GlobalVariableView));

        public static DependencyProperty DeselectAllButGlobalVariablesRequestProperty =
            DependencyProperty.Register(nameof(DeselectAllButGlobalVariablesRequest), typeof(ICommand), typeof(GlobalVariableView));

        public static DependencyProperty EditGlobalVariableCommandProperty =
            DependencyProperty.Register(nameof(EditGlobalVariableCommand), typeof(ICommand), typeof(GlobalVariableView));
        
        static GlobalVariableView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(GlobalVariableView), new FrameworkPropertyMetadata(typeof(GlobalVariableView)));
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

        public ICommand DeselectAllButGlobalVariablesRequest
        {
            get => (ICommand) GetValue(DeselectAllButGlobalVariablesRequestProperty);
            set => SetValue(DeselectAllButGlobalVariablesRequestProperty, value);
        }

        public ICommand EditGlobalVariableCommand
        {
            get => (ICommand) GetValue(EditGlobalVariableCommandProperty);
            set => SetValue(EditGlobalVariableCommandProperty, value);
        }
        

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.ClickCount == 1)
            {
                DeselectAllButGlobalVariablesRequest?.Execute(null);
               
                if (!IsSelected)
                {
                    if (!Keyboard.IsKeyDown(Key.LeftCtrl))
                        DeselectAllRequest?.Execute(null);
                    IsSelected = true;
                }
            }
            else if (e.ClickCount == 2)
                EditGlobalVariableCommand?.Execute(DataContext);
        }
    }
}