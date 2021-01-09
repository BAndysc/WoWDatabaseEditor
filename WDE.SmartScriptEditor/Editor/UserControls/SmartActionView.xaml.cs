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

namespace WDE.SmartScriptEditor.Editor.UserControls
{
    /// <summary>
    /// Interaction logic for SmartActionView.xaml
    /// </summary>
    public partial class SmartActionView : UserControl
    {
        public static DependencyProperty IsSelectedProperty
            = DependencyProperty.Register(
                nameof(IsSelected),
                typeof(bool),
                typeof(SmartActionView),
                new PropertyMetadata(false));

        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        public static DependencyProperty DeselectAllRequestProperty
            = DependencyProperty.Register(
                nameof(DeselectAllRequest),
                typeof(ICommand),
                typeof(SmartActionView));

        public ICommand DeselectAllRequest
        {
            get => (ICommand)GetValue(DeselectAllRequestProperty);
            set => SetValue(DeselectAllRequestProperty, value);
        }
        
        public static DependencyProperty DeselectAllEventsRequestProperty
            = DependencyProperty.Register(
                nameof(DeselectAllEventsRequest),
                typeof(ICommand),
                typeof(SmartActionView));

        public ICommand DeselectAllEventsRequest
        {
            get => (ICommand)GetValue(DeselectAllEventsRequestProperty);
            set => SetValue(DeselectAllEventsRequestProperty, value);
        }
        
        public static DependencyProperty EditActionCommandProperty
            = DependencyProperty.Register(
                nameof(EditActionCommand),
                typeof(ICommand),
                typeof(SmartActionView));

        public ICommand EditActionCommand
        {
            get => (ICommand)GetValue(EditActionCommandProperty);
            set => SetValue(EditActionCommandProperty, value);
        }
        
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.ClickCount == 1)
            {
                DeselectAllEventsRequest?.Execute(null);
                if (!IsSelected)
                {
                    if (!Keyboard.IsKeyDown(Key.LeftCtrl))
                        DeselectAllRequest?.Execute(null);
                    IsSelected = true;   
                }
            } else if (e.ClickCount == 2)
            {
                EditActionCommand?.Execute(DataContext);
            }
        }

        public SmartActionView()
        {
            InitializeComponent();
        }
    }
}
