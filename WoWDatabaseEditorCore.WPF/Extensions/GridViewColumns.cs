using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Xml;
using WDE.Common.Types;
using WoWDatabaseEditorCore.Extensions;

#nullable disable
namespace WoWDatabaseEditorCore.WPF.Extensions
{
    //https://stackoverflow.com/questions/2643545/wpf-mvvm-how-to-bind-gridviewcolumn-to-viewmodel-collection
    public static class GridViewColumns
    {
        // Using a DependencyProperty as the backing store for ColumnsSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ColumnsSourceProperty = DependencyProperty.RegisterAttached("ColumnsSource",
            typeof(object),
            typeof(GridViewColumns),
            new UIPropertyMetadata(null, ColumnsSourceChanged));

        // Using a DependencyProperty as the backing store for HeaderTextMember.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HeaderTextMemberProperty =
            DependencyProperty.RegisterAttached("HeaderTextMember",
                typeof(string),
                typeof(GridViewColumns),
                new UIPropertyMetadata(null));

        // Using a DependencyProperty as the backing store for DisplayMember.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DisplayMemberMemberProperty =
            DependencyProperty.RegisterAttached("DisplayMemberMember",
                typeof(string),
                typeof(GridViewColumns),
                new UIPropertyMetadata(null));

        public static readonly DependencyProperty CheckboxMemberProperty =
            DependencyProperty.RegisterAttached("CheckboxMember",
                typeof(string),
                typeof(GridViewColumns),
                new UIPropertyMetadata(null));

        public static readonly DependencyProperty ColumnHeadStyleNameProperty =
            DependencyProperty.RegisterAttached("ColumnHeadStyleName",
                typeof(string),
                typeof(GridViewColumns),
                new UIPropertyMetadata(null));

        private static readonly IDictionary<ICollectionView, List<GridView>> GridViewsByColumnsSource =
            new Dictionary<ICollectionView, List<GridView>>();

        [AttachedPropertyBrowsableForType(typeof(GridView))]
        public static object GetColumnsSource(DependencyObject obj)
        {
            return obj.GetValue(ColumnsSourceProperty);
        }

        public static void SetColumnsSource(DependencyObject obj, object value)
        {
            obj.SetValue(ColumnsSourceProperty, value);
        }


        [AttachedPropertyBrowsableForType(typeof(GridView))]
        public static string GetHeaderTextMember(DependencyObject obj)
        {
            return (string) obj.GetValue(HeaderTextMemberProperty);
        }

        public static void SetHeaderTextMember(DependencyObject obj, string value)
        {
            obj.SetValue(HeaderTextMemberProperty, value);
        }


        [AttachedPropertyBrowsableForType(typeof(GridView))]
        public static string GetDisplayMemberMember(DependencyObject obj)
        {
            return (string) obj.GetValue(DisplayMemberMemberProperty);
        }

        [AttachedPropertyBrowsableForType(typeof(GridView))]
        public static string GetCheckboxMember(DependencyObject obj)
        {
            return (string) obj.GetValue(CheckboxMemberProperty);
        }

        public static void SetDisplayMemberMember(DependencyObject obj, string value)
        {
            obj.SetValue(DisplayMemberMemberProperty, value);
        }

        [AttachedPropertyBrowsableForType(typeof(GridView))]
        public static string GetColumnHeadStyleName(DependencyObject obj)
        {
            return (string) obj.GetValue(ColumnHeadStyleNameProperty);
        }

        public static void SetColumnHeadStyleName(DependencyObject obj, string value)
        {
            obj.SetValue(ColumnHeadStyleNameProperty, value);
        }

        private static void ColumnsSourceChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            GridView gridView = obj as GridView;
            if (gridView != null)
            {
                gridView.Columns.Clear();

                if (e.OldValue != null)
                {
                    ICollectionView view = CollectionViewSource.GetDefaultView(e.OldValue);
                    //if (view != null)
                        //RemoveHandlers(gridView, view);
                }

                if (e.NewValue != null)
                {
                    ICollectionView view = CollectionViewSource.GetDefaultView(e.NewValue);
                    if (view != null)
                    {
                        //AddHandlers(gridView, view);
                        CreateColumns(gridView, view);
                    }
                }
            }
        }

        private static List<GridView> GetGridViewsForColumnSource(ICollectionView columnSource)
        {
            List<GridView> gridViews;
            if (!GridViewsByColumnsSource.TryGetValue(columnSource, out gridViews))
            {
                gridViews = new List<GridView>();
                GridViewsByColumnsSource.Add(columnSource, gridViews);
            }

            return gridViews;
        }

        private static void AddHandlers(GridView gridView, ICollectionView view)
        {
            GetGridViewsForColumnSource(view).Add(gridView);
            view.CollectionChanged += ColumnsSource_CollectionChanged;
        }

        private static void CreateColumns(GridView gridView, ICollectionView view)
        {
            foreach (object item in view)
            {
                GridViewColumn column = CreateColumn(gridView, item as ColumnDescriptor);

                gridView.Columns.Add(column);
            }
        }

        private static void RemoveHandlers(GridView gridView, ICollectionView view)
        {
            view.CollectionChanged -= ColumnsSource_CollectionChanged;
            GetGridViewsForColumnSource(view).Remove(gridView);
        }

        private static void ColumnsSource_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ICollectionView view = sender as ICollectionView;
            var gridViews = GetGridViewsForColumnSource(view);
            if (gridViews == null || gridViews.Count == 0)
                return;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (GridView gridView in gridViews)
                    {
                        for (var i = 0; i < e.NewItems.Count; i++)
                        {
                            GridViewColumn column = CreateColumn(gridView, e.NewItems[i] as ColumnDescriptor);
                            gridView.Columns.Insert(e.NewStartingIndex + i, column);
                        }
                    }

                    break;
                case NotifyCollectionChangedAction.Move:
                    foreach (GridView gridView in gridViews)
                    {
                        var columns = new List<GridViewColumn>();
                        for (var i = 0; i < e.OldItems.Count; i++)
                        {
                            GridViewColumn column = gridView.Columns[e.OldStartingIndex + i];
                            columns.Add(column);
                        }

                        for (var i = 0; i < e.NewItems.Count; i++)
                        {
                            GridViewColumn column = columns[i];
                            gridView.Columns.Insert(e.NewStartingIndex + i, column);
                        }
                    }

                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (GridView gridView in gridViews)
                    {
                        for (var i = 0; i < e.OldItems.Count; i++)
                            gridView.Columns.RemoveAt(e.OldStartingIndex);
                    }

                    break;
                case NotifyCollectionChangedAction.Replace:
                    foreach (GridView gridView in gridViews)
                    {
                        for (var i = 0; i < e.NewItems.Count; i++)
                        {
                            GridViewColumn column = CreateColumn(gridView, e.NewItems[i] as ColumnDescriptor);
                            gridView.Columns[e.NewStartingIndex + i] = column;
                        }
                    }

                    break;
                case NotifyCollectionChangedAction.Reset:
                    foreach (GridView gridView in gridViews)
                    {
                        gridView.Columns.Clear();
                        CreateColumns(gridView, sender as ICollectionView);
                    }

                    break;
            }
        }

        private static GridViewColumn CreateColumn(GridView gridView, ColumnDescriptor columnSource)
        {
            GridViewColumn column = new();
            
            string headerTextMember = GetHeaderTextMember(gridView);
            string displayMemberMember = GetDisplayMemberMember(gridView);
            bool checkbox = columnSource.CheckboxMember;
            string styleString = GetColumnHeadStyleName(gridView);
            column.Width = 90;
            if (!string.IsNullOrEmpty(headerTextMember))
                column.Header = GetPropertyValue(columnSource, headerTextMember);
            if (!string.IsNullOrEmpty(displayMemberMember) && !checkbox)
            {
                var propertyName = GetPropertyValue(columnSource, displayMemberMember) as string;
                var binding = new Binding(propertyName);
                if (columnSource.OneTime)
                    binding.Mode = BindingMode.OneTime;
                column.DisplayMemberBinding = binding;
            }
            else if (checkbox)
            {
                var propertyName = GetPropertyValue(columnSource, displayMemberMember) as string;
                StringReader stringReader = new(@"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""> 
                    <CheckBox IsChecked=""{Binding " + propertyName + @"}""/> 
                    </DataTemplate>");
                XmlReader xmlReader = XmlReader.Create(stringReader);
                column.CellTemplate = XamlReader.Load(xmlReader) as DataTemplate;
                column.Width = 24;
            }

            if (!string.IsNullOrEmpty(styleString))
            {
                Style style = Application.Current.FindResource(styleString) as Style;
                if (style != null)
                    column.HeaderContainerStyle = style;
            }

            if (columnSource.PreferredWidth.HasValue)
                column.Width = columnSource.PreferredWidth.Value;
            else
            {
                // Set Widht to NaN in order to stretch it to match conent
                column.Width = double.NaN;
            }
            return column;
        }

        private static object GetPropertyValue(object obj, string propertyName)
        {
            if (obj != null)
            {
                PropertyInfo prop = obj.GetType().GetProperty(propertyName);
                if (prop != null)
                    return prop.GetValue(obj, null);
            }

            return null;
        }
    }
}