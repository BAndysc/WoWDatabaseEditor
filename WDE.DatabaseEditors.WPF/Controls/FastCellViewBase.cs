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

namespace WDE.DatabaseEditors.WPF.Controls
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:WDE.DatabaseEditors.WPF.Controls"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:WDE.DatabaseEditors.WPF.Controls;assembly=WDE.DatabaseEditors.WPF.Controls"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Browse to and select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:FastCellViewBase/>
    ///
    /// </summary>
    public abstract class FastCellViewBase : Control
    {
        private static ContextMenu contextMenu;
        private static MenuItem revertMenuItem;
        private static MenuItem setNullMenuItem;
        private static MenuItem deleteMenuItem;
        private static MenuItem duplicateMenuItem;

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(object), typeof(FastCellViewBase));
        public static readonly DependencyProperty StringValueProperty = DependencyProperty.Register("StringValue", typeof(string), typeof(FastCellViewBase));
        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(FastCellViewBase));
        public static readonly DependencyProperty IsModifiedProperty = DependencyProperty.Register("IsModified", typeof(bool), typeof(FastCellViewBase), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register("IsActive", typeof(bool), typeof(FastCellViewBase));
        public static readonly DependencyProperty CanBeNullProperty = DependencyProperty.Register("CanBeNull", typeof(bool), typeof(FastCellViewBase));
        public static readonly DependencyProperty RevertCommandProperty = DependencyProperty.Register(nameof(RevertCommand), typeof(ICommand), typeof(FastCellViewBase));
        public static readonly DependencyProperty SetNullCommandProperty = DependencyProperty.Register(nameof(SetNullCommand), typeof(ICommand), typeof(FastCellViewBase));
        public static readonly DependencyProperty RemoveTemplateCommandProperty = DependencyProperty.Register(nameof(RemoveTemplateCommand), typeof(ICommand), typeof(FastCellViewBase));
        public static readonly DependencyProperty DuplicateCommandProperty = DependencyProperty.Register(nameof(DuplicateCommand), typeof(ICommand), typeof(FastCellViewBase));

        public ICommand? DuplicateCommand
        {
            get => (ICommand?)GetValue(DuplicateCommandProperty);
            set => SetValue(DuplicateCommandProperty, value);
        }
        public ICommand? RemoveTemplateCommand
        {
            get => (ICommand?)GetValue(RemoveTemplateCommandProperty);
            set => SetValue(RemoveTemplateCommandProperty, value);
        }
        public ICommand? SetNullCommand
        {
            get => (ICommand?)GetValue(SetNullCommandProperty);
            set => SetValue(SetNullCommandProperty, value);
        }
        public ICommand? RevertCommand
        {
            get => (ICommand?)GetValue(RevertCommandProperty);
            set => SetValue(RevertCommandProperty, value);
        }

        public bool CanBeNull
        {
            get => (bool)GetValue(CanBeNullProperty);
            set => SetValue(CanBeNullProperty, value);
        }
        public string StringValue
        {
            get => (string)GetValue(StringValueProperty);
            set => SetValue(StringValueProperty, value);
        }

        public object Value
        {
            get => (object)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set
            {
                SetValue(IsReadOnlyProperty, value);
                UpdateOpacity();
            }
        }

        private void UpdateOpacity()
        {
            Opacity = IsActive ? (IsReadOnly ? 0.5f : 1f) : 0f;
        }

        public bool IsModified
        {
            get => (bool)GetValue(IsModifiedProperty);
            set
            {
                SetValue(IsModifiedProperty, value);
                this.FontWeight = IsModified ? FontWeights.Bold : FontWeights.Normal;
            }
        }

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set
            {
                SetValue(IsActiveProperty, value);
                IsHitTestVisible = value;
                UpdateOpacity();
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (IsModified)
            {
                drawingContext.DrawRectangle(Brushes.Red, null, new Rect(0, 6, 12, 12));
            }
        }

        static FastCellViewBase()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FastCellViewBase), new FrameworkPropertyMetadata(typeof(FastCellViewBase)));

            // I am doing it in code behind for performance reason
            // I do not know why Avalonia allocates tooooons of memory
            // when it is in xaml...
            contextMenu = new ContextMenu();
            revertMenuItem = new MenuItem() { Header = "Revert value" };
            setNullMenuItem = new MenuItem() { Header = "Set to null" };
            duplicateMenuItem = new MenuItem() { Header = "Duplicate entity" };
            deleteMenuItem = new MenuItem() { Header = "Delete entity from the editor" };
            contextMenu.Items.Add(revertMenuItem);
            contextMenu.Items.Add(setNullMenuItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(duplicateMenuItem);
            contextMenu.Items.Add(deleteMenuItem);
            contextMenu.Closed += (sender, args) =>
            {
                revertMenuItem.CommandParameter = null!;
                setNullMenuItem.CommandParameter = null!;
                deleteMenuItem.CommandParameter = null!;
                duplicateMenuItem.CommandParameter = null!;
                revertMenuItem.Command = null;
                setNullMenuItem.Command = null;
                deleteMenuItem.Command = null;
                duplicateMenuItem.Command = null;
            };
        }

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseRightButtonDown(e);
            revertMenuItem.IsEnabled = RevertCommand?.CanExecute(DataContext!) ?? false;
            setNullMenuItem.IsEnabled = SetNullCommand?.CanExecute(DataContext!) ?? false;
            duplicateMenuItem.IsEnabled = DuplicateCommand?.CanExecute(DataContext!) ?? false;
            duplicateMenuItem.Visibility = DuplicateCommand == null ? Visibility.Collapsed : Visibility.Visible;
            revertMenuItem.CommandParameter = DataContext!;
            setNullMenuItem.CommandParameter = DataContext!;
            deleteMenuItem.CommandParameter = DataContext!;
            duplicateMenuItem.CommandParameter = DataContext!;
            revertMenuItem.Command = RevertCommand;
            setNullMenuItem.Command = SetNullCommand;
            deleteMenuItem.Command = RemoveTemplateCommand;
            duplicateMenuItem.Command = DuplicateCommand;
            contextMenu.PlacementTarget = this;
            contextMenu.IsOpen = true;
            e.Handled = true;
        }
    }
}
