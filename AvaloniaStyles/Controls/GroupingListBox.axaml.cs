using System;
using System.Collections;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Metadata;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace AvaloniaStyles.Controls
{
    public class GroupingHeader : TemplatedControl
    {
        public static readonly DirectProperty<GroupingHeader, string> GroupNameProperty =
            AvaloniaProperty.RegisterDirect<GroupingHeader, string>(
                nameof(GroupName),
                o => o.GroupName,
                (o, v) => o.GroupName = v);

        private string groupName;
        
        [Content]
        public string GroupName
        {
            get => groupName;
            set => SetAndRaise(GroupNameProperty, ref groupName, value);
        }
        
        public static readonly DirectProperty<GroupingHeader, IControl> CustomContentProperty =
            AvaloniaProperty.RegisterDirect<GroupingHeader, IControl>(
                nameof(CustomContent),
                o => o.CustomContent,
                (o, v) => o.CustomContent = v);

        private IControl customContent;
        public IControl CustomContent
        {
            get => customContent;
            set => SetAndRaise(CustomContentProperty, ref customContent, value);
        }
    }
    
    // yes, this implementation is quite dirty
    // this is in order to make SelectionItem working
    public class GroupingListBox : TemplatedControl
    {
        public static readonly DirectProperty<GroupingListBox, object?> SelectedItemProperty =
            AvaloniaProperty.RegisterDirect<GroupingListBox, object?>(
                nameof(SelectedItem),
                o => o.SelectedItem,
                (o, v) => o.SelectedItem = v,
                defaultBindingMode: BindingMode.TwoWay);

        private object _selectedItem;
        
        public object SelectedItem
        {
            get => _selectedItem;
            set => SetAndRaise(SelectedItemProperty, ref _selectedItem, value);
        }
        
        public static readonly DirectProperty<GroupingListBox, IEnumerable> ItemsProperty =
            AvaloniaProperty.RegisterDirect<GroupingListBox, IEnumerable>(nameof(Items), o => o.Items, (o, v) => o.Items = v);

        private IEnumerable _items = new AvaloniaList<object>();

        [Content]
        public IEnumerable Items
        {
            get => _items;
            set => SetAndRaise(ItemsProperty, ref _items, value);
        }
        
        private static readonly FuncTemplate<IPanel> DefaultPanel =
            new FuncTemplate<IPanel>(() => new StackPanel());
        
        public static readonly StyledProperty<ITemplate<IPanel>> ItemsPanelProperty =
            AvaloniaProperty.Register<GroupingListBox, ITemplate<IPanel>>(nameof(ItemsPanel), DefaultPanel);

        public static readonly StyledProperty<IDataTemplate> ItemTemplateProperty =
            AvaloniaProperty.Register<GroupingListBox, IDataTemplate>(nameof(ItemTemplate));
        
        public ITemplate<IPanel> ItemsPanel
        {
            get => GetValue(ItemsPanelProperty);
            set => SetValue(ItemsPanelProperty, value);
        }
        
        public IDataTemplate ItemTemplate
        {
            get => GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }
        
        public static readonly StyledProperty<IDataTemplate> HeaderTemplateProperty =
            AvaloniaProperty.Register<GroupingListBox, IDataTemplate>(nameof(HeaderTemplate));

        public IDataTemplate HeaderTemplate
        {
            get => GetValue(HeaderTemplateProperty);
            set => SetValue(HeaderTemplateProperty, value);
        }

        public GroupingListBox()
        {
            this.AddHandler(InputElement.KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
        }

        #region KEYBOARD_NAVIGATION
        private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Source is not ListBoxItem lbi)
                return;
            
            var that = lbi.FindAncestorOfType<ListBox>();
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
        #endregion
    }

    internal class GroupingListBoxInner : ListBox, IStyleable
    {
        Type IStyleable.StyleKey => typeof(ListBox);
        
        public static readonly DirectProperty<GroupingListBoxInner, object?> CustomSelectedItemProperty =
            AvaloniaProperty.RegisterDirect<GroupingListBoxInner, object?>(
                nameof(CustomSelectedItem),
                o => o.CustomSelectedItem,
                (o, v) =>
                {
                    if (!o.IsAttached())
                        return;
                    if (v == null || o.Items is IList list && list.Contains(v))
                        o.CustomSelectedItem = v;
                    else
                        o.CustomSelectedItem = null;
                },
                defaultBindingMode: BindingMode.TwoWay);

        public object CustomSelectedItem
        {
            get => SelectedItem;
            set
            {
                var old = SelectedItem;
                SelectedItem = value;
            }
        }

        private static bool inEvent;
        
        static GroupingListBoxInner()
        {
            SelectedItemProperty.Changed.AddClassHandler<GroupingListBoxInner>((o, arg) =>
            {
                if (!o.IsAttached())
                    return;

                if (inEvent)
                    return;
                
                inEvent = true;
                {
                    o.CustomSelectedItem = arg.NewValue;
                    GroupingListBox parent = o.FindAncestorOfType<GroupingListBox>();
                    if (parent != null)
                        parent.SelectedItem = arg.NewValue;
                    o.RaisePropertyChanged(CustomSelectedItemProperty, arg.OldValue, arg.NewValue);
                }

                inEvent = false;
            });
        }

        private bool IsAttached() => ((IVisual) this).IsAttachedToVisualTree;
    }
}