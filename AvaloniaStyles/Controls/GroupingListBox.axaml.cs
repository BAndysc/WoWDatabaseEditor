using System;
using System.Collections;
using System.Linq;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Metadata;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace AvaloniaStyles.Controls
{
    public class GroupingHeader : TemplatedControl
    {
        public static readonly DirectProperty<GroupingHeader, object?> GroupNameProperty =
            AvaloniaProperty.RegisterDirect<GroupingHeader, object?>(
                nameof(GroupName),
                o => o.GroupName,
                (o, v) => o.GroupName = v);

        private object? groupName = "";
        
        [Content]
        public object? GroupName
        {
            get => groupName;
            set => SetAndRaise(GroupNameProperty, ref groupName, value);
        }
        
        public static readonly DirectProperty<GroupingHeader, Control?> CustomContentProperty =
            AvaloniaProperty.RegisterDirect<GroupingHeader, Control?>(
                nameof(CustomContent),
                o => o.CustomContent,
                (o, v) => o.CustomContent = v);

        private Control? customContent;
        public Control? CustomContent
        {
            get => customContent;
            set => SetAndRaise(CustomContentProperty, ref customContent, value);
        }
        
        public static readonly DirectProperty<GroupingHeader, Control?> CustomRightContentProperty =
            AvaloniaProperty.RegisterDirect<GroupingHeader, Control?>(
                nameof(CustomRightContent),
                o => o.CustomRightContent,
                (o, v) => o.CustomRightContent = v);

        private Control? customRightContent;
        public Control? CustomRightContent
        {
            get => customRightContent;
            set => SetAndRaise(CustomRightContentProperty, ref customRightContent, value);
        }
        
        
        public static readonly DirectProperty<GroupingHeader, Control?> CustomCenterContentProperty =
            AvaloniaProperty.RegisterDirect<GroupingHeader, Control?>(
                nameof(CustomCenterContent),
                o => o.CustomCenterContent,
                (o, v) => o.CustomCenterContent = v);

        private Control? customCenterContent;
        public Control? CustomCenterContent
        {
            get => customCenterContent;
            set => SetAndRaise(CustomCenterContentProperty, ref customCenterContent, value);
        }
    }
    
    // yes, this implementation is quite dirty
    // this is in order to make SelectionItem working
    public class GroupingListBox : TemplatedControl
    {
        private ScrollViewer? scroll;
        private ItemsControl? parentItems;

        public static readonly DirectProperty<GroupingListBox, object?> SelectedItemProperty =
            AvaloniaProperty.RegisterDirect<GroupingListBox, object?>(
                nameof(SelectedItem),
                o => o.SelectedItem,
                (o, v) => o.SelectedItem = v,
                defaultBindingMode: BindingMode.TwoWay);

        private object? selectedItem;
        
        public object? SelectedItem
        {
            get => selectedItem;
            set => SetAndRaise(SelectedItemProperty, ref selectedItem, value);
        }
        
        public static readonly DirectProperty<GroupingListBox, IEnumerable> ItemsProperty =
            AvaloniaProperty.RegisterDirect<GroupingListBox, IEnumerable>(nameof(Items), o => o.Items, (o, v) => o.Items = v);

        private IEnumerable items = new AvaloniaList<object>();

        [Content]
        public IEnumerable Items
        {
            get => items;
            set => SetAndRaise(ItemsProperty, ref items, value);
        }
        
        private static readonly FuncTemplate<Panel> DefaultPanel =
            new FuncTemplate<Panel>(() => new StackPanel());
        
        public static readonly StyledProperty<ITemplate<Panel>> ItemsPanelProperty =
            AvaloniaProperty.Register<GroupingListBox, ITemplate<Panel>>(nameof(ItemsPanel), DefaultPanel);

        public static readonly StyledProperty<IDataTemplate> ItemTemplateProperty =
            AvaloniaProperty.Register<GroupingListBox, IDataTemplate>(nameof(ItemTemplate));
        
        public ITemplate<Panel> ItemsPanel
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

        public void ScrollToItem(object item)
        {
            if (Items is not IList collection)
                return;

            if (parentItems == null || scroll == null)
                return;
            
            int index = collection.IndexOf(item);
            if (index == -1)
                return;

            var container = parentItems.ContainerFromIndex(index);
            if (container != null)
                scroll.Offset = new Vector(0, container.Bounds.Y);
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            scroll = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");
            parentItems = e.NameScope.Find<ItemsControl>("PART_ParentItems");
        }

        public void FocusElement(int parentIndex, int innerIndex)
        {
            if (parentItems == null)
                return;

            if (parentIndex < 0 || parentIndex >= parentItems.ItemCount)
                return;

            var listBox = GetListBoxFromItemsControl(parentItems, parentIndex);
            if (listBox == null)
                return;

            if (innerIndex < 0 || innerIndex >= listBox.ItemCount)
                return;
            
            listBox.SelectedIndex = innerIndex;
            (listBox.ContainerFromIndex(listBox.SelectedIndex))?.Focus();
        }

        #region KEYBOARD_NAVIGATION
        private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Source is not ListBoxItem lbi)
                return;
            
            var that = lbi.FindAncestorOfType<ListBox>();
            if (that == null)
                return;
            
            var parent = FindParent<ItemsControl>(that);

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
                var element = that.ContainerFromIndex(0);
                if (element == null)
                    return;
                int itemsInRow = (int)(that.Bounds.Width / (element.Bounds.Width));
                MoveUpDown(that, parent, itemsInRow, dir);
            }   
        }

        private ListBox? GetNextParent(ItemsControl parent, ListBox current, int dir)
        {
            var currentParentIndex = ((IList?)parent.Items)?.IndexOf(current.DataContext) ?? -1;
            var nextParentIndex = currentParentIndex + dir;
            if (nextParentIndex < 0 || nextParentIndex > parent.ItemCount - 1)
                return null;
            return GetListBoxFromItemsControl(parent, nextParentIndex);
        }

        private ListBox? GetListBoxFromItemsControl(ItemsControl parent, int index)
        {
            var x = parent.ContainerFromIndex(index);
            if (x == null)
                return null;
            var logicalChildren = x.GetLogicalChildren().ToList();
            if (x == null || logicalChildren.Count == 0 || logicalChildren[0].LogicalChildren.Count < 2)
                return null;
            return (ListBox) logicalChildren[0].LogicalChildren[1];
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
            listbox.ContainerFromIndex(listbox.SelectedIndex)?.Focus();
        }
        
        public static T? FindParent<T>(Visual? obj) where T : Control
        {
            obj = obj?.GetVisualParent();
            while (obj != null)
            {
                if (obj is T parent)
                    return parent;
                obj = obj.GetVisualParent();
            }

            return null;
        }
        #endregion
    }

    internal class GroupingListBoxInner : ListBox
    {
        protected override Type StyleKeyOverride => typeof(ListBox);
        
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

        public object? CustomSelectedItem
        {
            get => SelectedItem;
            set => SelectedItem = value;
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
                    GroupingListBox? parent = o.FindAncestorOfType<GroupingListBox>();
                    if (parent != null)
                        parent.SelectedItem = arg.NewValue;
                    o.RaisePropertyChanged(CustomSelectedItemProperty, arg.OldValue, arg.NewValue);
                }

                inEvent = false;
            });
        }

        private bool IsAttached() => ((Visual) this).GetVisualRoot() != null;
    }
}