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

namespace WDE.Conditions.UserControls
{
    /// <summary>
    /// Interaction logic for ConditionListView.xaml
    /// </summary>
    public partial class ConditionListView : UserControl
    {
        public static DependencyProperty SelectionChangedCommandProperty
            = DependencyProperty.Register(
            "SelectionChangedCommand",
            typeof(ICommand),
            typeof(ConditionListView));

        public ICommand SelectionChangedCommand
        {
            get { return (ICommand)GetValue(SelectionChangedCommandProperty); }
            set { SetValue(SelectionChangedCommandProperty, value); }
        }

        public ConditionListView()
        {
            InitializeComponent();
        }

        private void ConditionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
                SelectionChangedCommand.Execute(e.AddedItems[0]);
        }
    }
}
