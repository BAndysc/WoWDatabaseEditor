using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Prism.Commands;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Editor.UserControls
{
    /// <summary>
    /// Interaction logic for SmartEventView.xaml
    /// </summary>
    public partial class SmartEventView : UserControl
    {
        public static DependencyProperty EditEventCommandProperty
            = DependencyProperty.Register(
            "EditEventCommand",
            typeof(ICommand),
            typeof(SmartEventView));

        public ICommand EditEventCommand
        {
            get
            {
                return (ICommand)GetValue(EditEventCommandProperty);
            }

            set
            {
                SetValue(EditEventCommandProperty, value);
            }
        }

        public static DependencyProperty EditActionCommandProperty
            = DependencyProperty.Register(
            "EditActionCommand",
            typeof(ICommand),
            typeof(SmartEventView));

        public ICommand EditActionCommand
        {
            get
            {
                return (ICommand)GetValue(EditActionCommandProperty);
            }

            set
            {
                SetValue(EditActionCommandProperty, value);
            }
        }

        public static DependencyProperty AddActionCommandProperty
            = DependencyProperty.Register(
            "AddActionCommand",
            typeof(ICommand),
            typeof(SmartEventView));

        public ICommand AddActionCommand
        {
            get
            {
                return (ICommand)GetValue(AddActionCommandProperty);
            }

            set
            {
                SetValue(AddActionCommandProperty, value);
            }
        }

        public static DependencyProperty DeleteActionCommandProperty
            = DependencyProperty.Register(
            "DeleteActionCommand",
            typeof(ICommand),
            typeof(SmartEventView));

        public ICommand DeleteActionCommand
        {
            get { return (ICommand)GetValue(DeleteActionCommandProperty); }
            set { SetValue(DeleteActionCommandProperty, value); }
        }

        public SmartEventView()
        {
            InitializeComponent();

            ActionsVieww.EditActionCommand = new DelegateCommand<SmartAction>(EditAction);
            ActionsVieww.DeleteSmartActionCommand = new DelegateCommand<SmartAction>(DeleteAction);
        }

        private void DeleteAction(SmartAction obj)
        {
            DeleteActionCommand?.Execute(obj);
        }

        private void EditAction(SmartAction action)
        {
            EditActionCommand?.Execute(action);
        }

        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                EditEventCommand?.Execute(DataContext);
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            AddActionCommand?.Execute(this.DataContext);
        }
    }
}
