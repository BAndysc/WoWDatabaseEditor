using System;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;
using WDE.DatabaseEditors.Avalonia.Views.MultiRow;

namespace WDE.DatabaseEditors.Avalonia.Controls
{
    public abstract class FastCellViewBase : TemplatedControl
    {
        private static ContextMenu contextMenu;
        private static MenuItem revertMenuItem;
        private static MenuItem setNullMenuItem;
        private static MenuItem deleteMenuItem;
        private static MenuItem duplicateMenuItem;
        
        protected object cellValue = "(null)";
        public static readonly DirectProperty<FastCellViewBase, object> ValueProperty = AvaloniaProperty.RegisterDirect<FastCellViewBase, object>("Value", o => o.Value, (o, v) => o.Value = v, defaultBindingMode: BindingMode.TwoWay);
        protected string stringValue = "(null)";
        public static readonly DirectProperty<FastCellViewBase, string> StringValueProperty = AvaloniaProperty.RegisterDirect<FastCellViewBase, string>("StringValue", o => o.StringValue, (o, v) => o.StringValue = v);
        protected bool isReadOnly;
        public static readonly DirectProperty<FastCellViewBase, bool> IsReadOnlyProperty = AvaloniaProperty.RegisterDirect<FastCellViewBase, bool>("IsReadOnly", o => o.IsReadOnly, (o, v) => o.IsReadOnly = v);
        protected bool isModified;
        public static readonly DirectProperty<FastCellViewBase, bool> IsModifiedProperty = AvaloniaProperty.RegisterDirect<FastCellViewBase, bool>("IsModified", o => o.IsModified, (o, v) => o.IsModified = v);
        protected bool isActive;
        public static readonly DirectProperty<FastCellViewBase, bool> IsActiveProperty = AvaloniaProperty.RegisterDirect<FastCellViewBase, bool>("IsActive", o => o.IsActive, (o, v) => o.IsActive = v);
        private bool canBeNull;
        public static readonly DirectProperty<FastCellViewBase, bool> CanBeNullProperty = AvaloniaProperty.RegisterDirect<FastCellViewBase, bool>("CanBeNull", o => o.CanBeNull, (o, v) => o.CanBeNull = v);
        private ICommand? revertCommand;
        public static readonly DirectProperty<FastCellViewBase, ICommand?> RevertCommandProperty = AvaloniaProperty.RegisterDirect<FastCellViewBase, ICommand?>(nameof(RevertCommand), o => o.RevertCommand, (o, v) => o.RevertCommand = v);
        private ICommand? setNullCommand;
        public static readonly DirectProperty<FastCellViewBase, ICommand?> SetNullCommandProperty = AvaloniaProperty.RegisterDirect<FastCellViewBase, ICommand?>(nameof(SetNullCommand), o => o.SetNullCommand, (o, v) => o.SetNullCommand = v);
        private ICommand? removeTemplateCommand;
        public static readonly DirectProperty<FastCellViewBase, ICommand?> RemoveTemplateCommandProperty = AvaloniaProperty.RegisterDirect<FastCellViewBase, ICommand?>(nameof(RemoveTemplateCommand), o => o.RemoveTemplateCommand, (o, v) => o.RemoveTemplateCommand = v);
        private ICommand? duplicateCommand;
        public static readonly DirectProperty<FastCellViewBase, ICommand?> DuplicateCommandProperty = AvaloniaProperty.RegisterDirect<FastCellViewBase, ICommand?>(nameof(DuplicateCommand), o => o.DuplicateCommand, (o, v) => o.DuplicateCommand = v);
        private ICommand? chooseParameterCommand;
        public static readonly DirectProperty<FastCellViewBase, ICommand?> ChooseParameterCommandProperty = AvaloniaProperty.RegisterDirect<FastCellViewBase, ICommand?>("ChooseParameterCommand", o => o.ChooseParameterCommand, (o, v) => o.ChooseParameterCommand = v);
        
        public ICommand? ChooseParameterCommand
        {
            get => chooseParameterCommand;
            set => SetAndRaise(ChooseParameterCommandProperty, ref chooseParameterCommand, value);
        }
        public ICommand? DuplicateCommand
        {
            get => duplicateCommand;
            set => SetAndRaise(DuplicateCommandProperty, ref duplicateCommand, value);
        }
        public ICommand? RemoveTemplateCommand
        {
            get => removeTemplateCommand;
            set => SetAndRaise(RemoveTemplateCommandProperty, ref removeTemplateCommand, value);
        }
        public ICommand? SetNullCommand
        {
            get => setNullCommand;
            set => SetAndRaise(SetNullCommandProperty, ref setNullCommand, value);
        }
        public ICommand? RevertCommand
        {
            get => revertCommand;
            set => SetAndRaise(RevertCommandProperty, ref revertCommand, value);
        }
        
        public bool CanBeNull
        {
            get => canBeNull;
            set => SetAndRaise(CanBeNullProperty, ref canBeNull, value);
        }
        public string StringValue
        {
            get => stringValue;
            set => SetAndRaise(StringValueProperty, ref stringValue, value);
        }

        public object Value
        {
            get => cellValue;
            set => SetAndRaise(ValueProperty, ref cellValue, value);
        }
        
        public bool IsReadOnly
        {
            get => isReadOnly;
            set
            {
                SetAndRaise(IsReadOnlyProperty, ref isReadOnly, value);
                UpdateOpacity();
            }
        }

        private void UpdateOpacity()
        {
            Opacity = isActive ? (isReadOnly ? 0.5f : 1f) : 0f;
        }

        public bool IsModified
        {
            get => isModified;
            set
            {
                SetAndRaise(IsModifiedProperty, ref isModified, value);
                this.FontWeight = isModified ? FontWeight.Bold : FontWeight.Normal;
            }
        }

        public bool IsActive
        {
            get => isActive;
            set
            {
                SetAndRaise(IsActiveProperty, ref isActive, value);
                IsHitTestVisible = value;
                UpdateOpacity();
            }
        }
        
        static FastCellViewBase()
        {
            AffectsRender<FastCellViewBase>(IsModifiedProperty);

            // I am doing it in code behind for performance reason
            // I do not know why Avalonia allocates tooooons of memory
            // when it is in xaml...
            contextMenu = new ContextMenu();
            revertMenuItem = new MenuItem() {Header = "Revert value"};
            setNullMenuItem = new MenuItem() {Header = "Set to null"};
            deleteMenuItem = new MenuItem() {Header = "Unload entity from the editor"};
            duplicateMenuItem = new MenuItem() {Header = "Duplicate row"};
            contextMenu.ItemsSource = new Control[]
            {
                revertMenuItem,
                setNullMenuItem,
                new Separator(),
                duplicateMenuItem,
                deleteMenuItem
            };
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

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            if (isModified)
            {
                context.DrawRectangle(Brushes.Orange, null, new Rect(0, 6, 12, 12));
            }
        }

        private bool MoveLeft(Key key, KeyModifiers modifiers, bool leftRight)
        {
            return (key == Key.Tab && (modifiers & KeyModifiers.Shift) != 0) ||
                   (key == Key.Left && (leftRight || (modifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0));
        }

        private bool MoveRight(Key key, KeyModifiers modifiers, bool leftRight)
        {
            return (key == Key.Tab && (modifiers & KeyModifiers.Shift) == 0) ||
                   (key == Key.Right && (leftRight || (modifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0));
        }

        private bool MoveDown(Key key, KeyModifiers modifiers)
        {
            return key == Key.Down || key == Key.Tab;
        }
        
        private bool MoveUp(Key key, KeyModifiers modifiers)
        {
            return key == Key.Up;
        }
        
        protected virtual void EndEditing(bool commit = true, bool focus = true) {}
        
        protected void HandleMoveLeftRightUpBottom(KeyEventArgs args, bool leftRight)
        {
            if (MoveLeft(args.Key, args.KeyModifiers, leftRight) || MoveRight(args.Key, args.KeyModifiers, leftRight))
            {
                var wrapper = (this.GetVisualParent()?.GetVisualParent()?.GetVisualParent() as ContentPresenter) ?? this.GetVisualParent() as ContentPresenter;

                var itemsPresenter = wrapper?.FindAncestorOfType<ItemsControl>();
                if (itemsPresenter == null)
                    return;

                if (wrapper == null)
                    return;

                var index = itemsPresenter.IndexFromContainer(wrapper);

                if (MoveLeft(args.Key, args.KeyModifiers, leftRight))
                    index--;
                else
                    index++;

                var next = itemsPresenter.ContainerFromIndex(index)?.FindDescendantOfType<FastCellViewBase>();

                if (next != null)
                {
                    EndEditing();
                    next.Focus(NavigationMethod.Tab);
                    args.Handled = true;
                }
            }

            if (MoveUp(args.Key, args.KeyModifiers) || MoveDown(args.Key, args.KeyModifiers))
            {
                var wrapper = this.GetVisualParent() as Control;
                var itemsPresenter = wrapper?.GetLogicalParent() as ItemsControl;
                var row = itemsPresenter?.GetVisualParent()?.GetVisualParent() as Control;
                var rows = row?.GetLogicalParent() as ItemsControl;
                if (wrapper == null || itemsPresenter == null || row == null || rows == null)
                    return;
                
                var innerIndex = itemsPresenter.IndexFromContainer(wrapper);
                var rowIndex = rows.IndexFromContainer(row);

                if (MoveDown(args.Key, args.KeyModifiers))
                    rowIndex++;
                else
                    rowIndex--;

                var newRow = rows.ContainerFromIndex(rowIndex).FindDescendantOfType<ItemsControl>();

                if (newRow == null)
                    return;

                var newCell = newRow.ContainerFromIndex(innerIndex)?.FindDescendantOfType<FastCellViewBase>();

                if (newCell == null)
                    return;

                this.EndEditing(focus: false);
                newCell.Focus(NavigationMethod.Tab);
                args.Handled = true;
            }
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            {
                revertMenuItem.IsEnabled = revertCommand?.CanExecute(DataContext!) ?? false;
                setNullMenuItem.IsEnabled = setNullCommand?.CanExecute(DataContext!) ?? false;
                duplicateMenuItem.IsVisible = duplicateCommand != null;
                duplicateMenuItem.IsEnabled = duplicateCommand?.CanExecute(DataContext!) ?? false;
                revertMenuItem.CommandParameter = DataContext!;
                setNullMenuItem.CommandParameter = DataContext!;
                deleteMenuItem.CommandParameter = DataContext!;
                duplicateMenuItem.CommandParameter = DataContext!;
                revertMenuItem.Command = revertCommand;
                setNullMenuItem.Command = setNullCommand;
                deleteMenuItem.Command = removeTemplateCommand;
                duplicateMenuItem.Command = duplicateCommand;
                contextMenu.Open(this);
                e.Handled = true;
            }
        }
    }
}
