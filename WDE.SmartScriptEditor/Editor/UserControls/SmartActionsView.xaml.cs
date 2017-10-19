using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Editor.UserControls
{
    /// <summary>
    /// Interaction logic for SmartActionsView.xaml
    /// </summary>
    public partial class SmartActionsView : UserControl
    {
        public static DependencyProperty EditActionCommandProperty
            = DependencyProperty.Register(
            "EditActionCommand",
            typeof(ICommand),
            typeof(SmartActionsView));

        public ICommand EditActionCommand
        {
            get { return (ICommand)GetValue(EditActionCommandProperty);}
            set { SetValue(EditActionCommandProperty, value);}
        }

        public static DependencyProperty DeleteActionCommandProperty
            = DependencyProperty.Register(
            "DeleteSmartActionCommand",
            typeof(ICommand),
            typeof(SmartActionsView));

        public ICommand DeleteSmartActionCommand
        {
            get { return (ICommand)GetValue(DeleteActionCommandProperty); }
            set { SetValue(DeleteActionCommandProperty, value); }
        }

        public SmartAction Selected { get; set; }

        public SmartActionsView()
        {
            InitializeComponent();
            
            Binding myBinding = new Binding();
            myBinding.Source = this;
            myBinding.Path = new PropertyPath("Selected");
            myBinding.Mode = BindingMode.TwoWay;
            myBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(ActionList, Selector.SelectedItemProperty, myBinding);
        }

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
