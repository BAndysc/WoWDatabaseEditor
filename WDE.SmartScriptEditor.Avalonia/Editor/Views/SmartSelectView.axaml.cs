using System.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using WDE.Common.Avalonia.Controls;

namespace WDE.SmartScriptEditor.Avalonia.Editor.Views
{
    /// <summary>
    ///     Interaction logic for SmartSelectView.xaml
    /// </summary>
    public partial class SmartSelectView : DialogViewBase
    {
        public SmartSelectView()
        {
            InitializeComponent();
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        private void InputElement_OnKeyDown(object? sender, KeyEventArgs e)
        {
            var that = sender as ListBox;
            if (that == null)
                return;
            var parent = FindParent<ItemsControl>(that!);

            if (parent == null)
                return;
            
            if (e.Key == Key.Left || e.Key == Key.Right)
            {
                e.Handled = true;
                MoveNextPrev(that, parent, e.Key == Key.Left ? -1 : 1);
            }
            else if (e.Key == Key.Up || e.Key == Key.Down)
            {
                e.Handled = true;
                var dir = e.Key == Key.Up ? -1 : 1;
                int itemsInRow = (int)(that.Bounds.Width / ((that.ItemContainerGenerator.ContainerFromIndex(0)).Bounds.Width));
                MoveUpDown(that, parent, itemsInRow, dir);
            }
        }

        private ListBox? GetNextParent(ItemsControl parent, ListBox current, int dir)
        {
            var currentParentIndex = ((IList)parent.Items).IndexOf(current.DataContext);
            var nextParentIndex = currentParentIndex + dir;
            if (nextParentIndex < 0 || nextParentIndex > parent.ItemCount - 1)
                return null;
            var x = parent.ItemContainerGenerator.ContainerFromIndex(nextParentIndex);
            if (x == null || x.LogicalChildren.Count == 0 || x.LogicalChildren[0].LogicalChildren.Count < 2)
                return null;
            return (ListBox) x.LogicalChildren[0].LogicalChildren[1];
        }
        
        private void MoveNextPrev(ListBox listBox, ItemsControl parent, int dir)
        {
            ListBox? nextSelectedListBox = listBox;
            int nextSelectedIndex = listBox.SelectedIndex + dir;

            if (nextSelectedIndex < 0 || nextSelectedIndex >= nextSelectedListBox.ItemCount)
            {
                nextSelectedListBox = GetNextParent(parent, listBox, dir);
                
                if (nextSelectedListBox == null)
                    return;
                
                if (nextSelectedIndex < 0)
                    nextSelectedIndex = nextSelectedListBox.ItemCount - 1;
                else
                    nextSelectedIndex = 0;
            }

            SelectIndex(nextSelectedListBox, nextSelectedIndex);
        }

        private bool MoveUpDown(ListBox? listBox, ItemsControl parent, int columns, int dir)
        {
            if (listBox == null)
                return false;
            int column = listBox.SelectedIndex % columns;
            
            int newIndex = FindIndexInColumn(listBox.SelectedIndex, listBox.ItemCount, column, columns, dir);
            if (newIndex != -1)
            {
                SelectIndex(listBox, newIndex);
                return true;
            }
            listBox = GetNextParent(parent, listBox, dir);

            while (listBox != null)
            {
                int index = FindIndexInColumn(dir > 0 ? -1 : listBox.ItemCount, listBox.ItemCount, column, columns, dir);
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
            (listbox.ItemContainerGenerator.ContainerFromIndex(listbox.SelectedIndex)).Focus();
        }
        
        public static T? FindParent<T>(IVisual obj) where T : Control
        {
            obj = obj.VisualParent;
            while (obj != null)
            {
                if (obj is T parent)
                    return parent;
                obj = obj.VisualParent;
            }

            return null;
        }
    }
}