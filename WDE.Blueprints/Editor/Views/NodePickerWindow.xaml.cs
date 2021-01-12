using System;
using System.Windows;

namespace WDE.Blueprints.Editor.Views
{
    /// <summary>
    ///     Interaction logic for NodePickerWindow.xaml
    /// </summary>
    public partial class NodePickerWindow : Window
    {
        public NodePickerWindow()
        {
            InitializeComponent();
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            Close();
        }
    }
}