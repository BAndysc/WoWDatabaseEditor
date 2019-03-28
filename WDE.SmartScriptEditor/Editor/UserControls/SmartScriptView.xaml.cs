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

using WDE.SmartScriptEditor.Editor.ViewModels;
using WDE.SmartScriptEditor.Editor.Helpers;

namespace WDE.SmartScriptEditor.Editor.UserControls
{
    /// <summary>
    /// Interaction logic for SmartScriptView.xaml
    /// </summary>
    public partial class SmartScriptView : UserControl
    {
        public static DependencyProperty DeleteEventCommandProperty
            = DependencyProperty.Register(
            "DeleteEventCommand",
            typeof(ICommand),
            typeof(SmartScriptView));

        private SmartScriptDrawer saiDrawer;

        public ICommand DeleteEventCommand
        {
            get { return (ICommand)GetValue(DeleteEventCommandProperty); }
            set { SetValue(DeleteEventCommandProperty, value); }
        }

        public SmartScriptView()
        {
            InitializeComponent();
        }

        private void EventSetter_OnHandler(object sender, MouseButtonEventArgs e)
        {

        }

        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            
        }

        private void UIElement_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                DeleteEventCommand?.Execute(this);
            }
        }

        protected override void OnRender(DrawingContext context)
        {
            if (saiDrawer == null)
            {
                SmartScriptEditorViewModel model = DataContext as SmartScriptEditorViewModel;
                saiDrawer = new SmartScriptDrawer(model.Events, scriptArea, model);
            }
            
            saiDrawer.Draw();
        }
    }
}
