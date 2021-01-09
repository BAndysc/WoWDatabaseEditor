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
            get => (ICommand)GetValue(EditEventCommandProperty);
            set => SetValue(EditEventCommandProperty, value);
        }


        public static DependencyProperty DeselectAllRequestProperty
            = DependencyProperty.Register(
                nameof(DeselectAllRequest),
                typeof(ICommand),
                typeof(SmartEventView));

        public ICommand DeselectAllRequest
        {
            get => (ICommand)GetValue(DeselectAllRequestProperty);
            set => SetValue(DeselectAllRequestProperty, value);
        }

        public static DependencyProperty DeselectActionsOfDeselectedEventsRequestProperty
            = DependencyProperty.Register(
                nameof(DeselectActionsOfDeselectedEventsRequest),
                typeof(ICommand),
                typeof(SmartEventView));

        public ICommand DeselectActionsOfDeselectedEventsRequest
        {
            get => (ICommand)GetValue(DeselectActionsOfDeselectedEventsRequestProperty);
            set => SetValue(DeselectActionsOfDeselectedEventsRequestProperty, value);
        }

        public static DependencyProperty IsSelectedProperty
            = DependencyProperty.Register(
                nameof(IsSelected),
                typeof(bool),
                typeof(SmartEventView),
                new PropertyMetadata(false));

        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }
        
        public SmartEventView()
        {
            InitializeComponent();
        }
        
        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                DeselectActionsOfDeselectedEventsRequest?.Execute(null);
                if (!IsSelected)
                {
                    if (!Keyboard.IsKeyDown(Key.LeftCtrl))
                        DeselectAllRequest?.Execute(null);
                    IsSelected = true;   
                }
            }
            else if (e.ClickCount == 2)
                EditEventCommand?.Execute(DataContext);
        }

    }
}
