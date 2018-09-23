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

namespace WDE.Blueprints.Editor.Views
{
    /// <summary>
    /// Interaction logic for NodePicker.xaml
    /// </summary>
    public partial class NodePicker : UserControl
    {
        public NodePicker()
        {
            InitializeComponent();
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                    if (ListPicker.SelectedIndex > 0)
                        ListPicker.SelectedIndex--;
                    break;

                case Key.Down:
                    if (ListPicker.SelectedIndex < ListPicker.Items.Count - 1)
                        ListPicker.SelectedIndex++;
                    break;

                case Key.Enter:
                    //if (ListPicker.SelectedItem == null)
                    //{
                    //    if (ListPicker.Items.Count == 0)
                    //        ViewModel.GraphModel.Pick(null);
                    //    else
                    //        ViewModel.GraphModel.Pick((ListPicker.Items[0] as NodeDefinition));
                    //}
                    //else
                    //    ViewModel.GraphModel.Pick((ListPicker.SelectedItem as NodeDefinition));
                    break;
            }
        }

        private void TextBlock_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //ViewModel.GraphModel.Pick(((sender as TextBlock).DataContext as NodeDefinition));
        }
    }
}
