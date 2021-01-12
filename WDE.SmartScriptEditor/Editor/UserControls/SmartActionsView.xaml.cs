using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Editor.UserControls
{
    /// <summary>
    ///     Interaction logic for SmartActionsView.xaml
    /// </summary>
    public partial class SmartActionsView : UserControl
    {
        public static DependencyProperty EditActionCommandProperty =
            DependencyProperty.Register("EditActionCommand", typeof(ICommand), typeof(SmartActionsView));

        public static DependencyProperty DeleteActionCommandProperty =
            DependencyProperty.Register("DeleteSmartActionCommand", typeof(ICommand), typeof(SmartActionsView));

        public SmartActionsView()
        {
            InitializeComponent();

            Binding myBinding = new();
            myBinding.Source = this;
            myBinding.Path = new PropertyPath("Selected");
            myBinding.Mode = BindingMode.TwoWay;
            myBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(ActionList, Selector.SelectedItemProperty, myBinding);
        }

        public ICommand EditActionCommand
        {
            get => (ICommand) GetValue(EditActionCommandProperty);
            set => SetValue(EditActionCommandProperty, value);
        }

        public ICommand DeleteSmartActionCommand
        {
            get => (ICommand) GetValue(DeleteActionCommandProperty);
            set => SetValue(DeleteActionCommandProperty, value);
        }

        public SmartAction Selected { get; set; }

        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                EditActionCommand?.Execute(Selected);
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
        }

        private void ActionList_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && Selected != null)
                DeleteSmartActionCommand?.Execute(Selected);
        }
    }
}