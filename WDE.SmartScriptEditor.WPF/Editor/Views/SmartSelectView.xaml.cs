using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WDE.SmartScriptEditor.Editor.ViewModels;

namespace WDE.SmartScriptEditor.WPF.Editor.Views
{
    /// <summary>
    ///     Interaction logic for SmartSelectView.xaml
    /// </summary>
    public partial class SmartSelectView : UserControl
    {
        public SmartSelectView()
        {
            InitializeComponent();
        }

        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && DataContext is SmartSelectViewModel vm && vm.Accept.CanExecute())
                vm.Accept.Execute();
        }

        // this messy code-behind enables moving between elements with arrows
        private void UIElement_OnKeyDown(object sender, KeyEventArgs e)
        {
            var that = sender as ListBox;
            var parent = FindParent<ItemsControl>(that);

            if (e.Key == Key.Left || e.Key == Key.Right)
            {
                e.Handled = true;
                MoveNextPrev(that, parent, e.Key == Key.Left ? -1 : 1);
            }
            else if (e.Key == Key.Up || e.Key == Key.Down)
            {
                e.Handled = true;
                var dir = e.Key == Key.Up ? -1 : 1;
                int itemsInRow = (int)(that.ActualWidth / (((FrameworkElement) that.ItemContainerGenerator.ContainerFromIndex(0)).ActualWidth));
                MoveUpDown(that, parent, itemsInRow, dir);
            }
        }

        private ListBox GetNextParent(ItemsControl parent, ListBox current, int dir)
        {
            var currentParentIndex = parent.Items.IndexOf(current.DataContext);
            var nextParentIndex = currentParentIndex + dir;
            if (nextParentIndex < 0 || nextParentIndex > parent.Items.Count - 1)
                return null;
            return (ListBox) VisualTreeHelper.GetChild(
                VisualTreeHelper.GetChild(parent.ItemContainerGenerator.ContainerFromItem(parent.Items[nextParentIndex]), 0),
                1);
        }
        
        private void MoveNextPrev(ListBox listBox, ItemsControl parent, int dir)
        {
            ListBox nextSelectedListBox = listBox;
            int nextSelectedIndex = listBox.SelectedIndex + dir;

            if (nextSelectedIndex < 0 || nextSelectedIndex >= nextSelectedListBox.Items.Count)
            {
                nextSelectedListBox = GetNextParent(parent, listBox, dir);
                
                if (nextSelectedListBox == null)
                    return;
                
                if (nextSelectedIndex < 0)
                    nextSelectedIndex = nextSelectedListBox.Items.Count - 1;
                else
                    nextSelectedIndex = 0;
            }

            SelectIndex(nextSelectedListBox, nextSelectedIndex);
        }

        private bool MoveUpDown(ListBox listBox, ItemsControl parent, int columns, int dir)
        {
            int column = listBox.SelectedIndex % columns;
            
            int newIndex = FindIndexInColumn(listBox.SelectedIndex, listBox.Items.Count, column, columns, dir);
            if (newIndex != -1)
            {
                SelectIndex(listBox, newIndex);
                return true;
            }
            listBox = GetNextParent(parent, listBox, dir);

            while (listBox != null)
            {
                int index = FindIndexInColumn(dir > 0 ? -1 : listBox.Items.Count, listBox.Items.Count, column, columns, dir);
                if (index != -1)
                {
                    SelectIndex(listBox, index);
                    return true;
                }
                listBox = GetNextParent(parent, listBox, dir);
            }

            return false;
        }

        private int FindIndexInColumn(int startIndex, int length, int column, int columns, int dir)
        {
            startIndex += dir;
            while ((dir > 0 && startIndex < length) || (dir < 0 && startIndex >= 0))
            {
                if (startIndex % columns == column)
                    return startIndex;
                startIndex += dir;
            }

            return -1;
        }

        private void SelectIndex(ListBox listbox, int index)
        {
            listbox.SelectedIndex = index;
            ((FrameworkElement) listbox.ItemContainerGenerator.ContainerFromIndex(listbox.SelectedIndex)).Focus();
        }
        
        public static T FindParent<T>(DependencyObject obj) where T : DependencyObject
        {
            obj = VisualTreeHelper.GetParent(obj);
            while (obj != null)
            {
                if (obj is T parent)
                    return parent;
                obj = VisualTreeHelper.GetParent(obj);
            }

            return null;
        }
    }
}