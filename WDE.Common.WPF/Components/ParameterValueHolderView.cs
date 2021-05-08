using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WDE.Common.WPF.Components
{
    public class ParameterValueHolderView : Control
    {
        public ICommand PickCommand
        {
            get => (ICommand)GetValue(PickCommandProperty);
            set => SetValue(PickCommandProperty, value);
        }

        public static readonly DependencyProperty PickCommandProperty =
            DependencyProperty.Register("PickCommand", typeof(ICommand), typeof(ParameterValueHolderView));

        static ParameterValueHolderView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ParameterValueHolderView), new FrameworkPropertyMetadata(typeof(ParameterValueHolderView)));
        }
    }
}
