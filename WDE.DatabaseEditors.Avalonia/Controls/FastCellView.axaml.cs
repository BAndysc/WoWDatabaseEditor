using System.Globalization;
using System.Reactive.Disposables;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace WDE.DatabaseEditors.Avalonia.Controls
{
    public class FastCellView : FastCellViewBase
    {
        private bool showChooseButton;
        public static readonly DirectProperty<FastCellView, bool> ShowChooseButtonProperty = AvaloniaProperty.RegisterDirect<FastCellView, bool>("ShowChooseButton", o => o.ShowChooseButton, (o, v) => o.ShowChooseButton = v);
        private ICommand? chooseParameterCommand;
        public static readonly DirectProperty<FastCellView, ICommand?> ChooseParameterCommandProperty = AvaloniaProperty.RegisterDirect<FastCellView, ICommand?>("ChooseParameterCommand", o => o.ChooseParameterCommand, (o, v) => o.ChooseParameterCommand = v);

        public ICommand? ChooseParameterCommand
        {
            get => chooseParameterCommand;
            set => SetAndRaise(ChooseParameterCommandProperty, ref chooseParameterCommand, value);
        }
        
        public bool ShowChooseButton
        {
            get => showChooseButton;
            set => SetAndRaise(ShowChooseButtonProperty, ref showChooseButton, value);
        }

        private Panel? partPanel;
        private TextBlock? partText;

        private System.IDisposable? subscriptionsOnOpen;

        private TextBox? textBox;
        private AdornerLayer? adornerLayer;
        private bool opened = false;

        private System.IDisposable? textBoxDisposable;

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            partPanel = e.NameScope.Find<Panel>("PART_Panel");
            partText = e.NameScope.Find<TextBlock>("PART_text");
            
            if (!isReadOnly)
                partText.Cursor = new Cursor(StandardCursorType.Ibeam);
            
            if (showChooseButton && !isReadOnly)
            {
                var chooseButton = new Button()
                {
                    Content = "...",
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Width = 30,
                    Command = ChooseParameterCommand,
                    CommandParameter = DataContext
                };
                    
                partPanel.Children.Add(chooseButton);
            }
        }
        
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            if (isReadOnly || e.Handled)
                return;

            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                return;
            
            if (e.Source != this && e.Source != partPanel && e.Source != partText)
                return;

            e.Handled = true;
            OpenForEditing();
        }

        private void EndEditing(bool commit = true)
        {
            textBoxDisposable?.Dispose();
            textBoxDisposable = null;
            if (textBox != null && commit)
            {
                if (Value is long)
                {
                    if (long.TryParse(textBox.Text, out var value))
                        Value = value;
                }
                else if (Value is float)
                {
                    if (float.TryParse(textBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                        Value = value;
                }
                else
                    Value = textBox.Text;
            }
            
            if (textBox != null && adornerLayer != null)
                adornerLayer.Children.Remove(textBox);
            
            Opacity = 1;
            subscriptionsOnOpen?.Dispose();
            subscriptionsOnOpen = null;
            textBox = null;
            adornerLayer = null;
            opened = false;
        }

        private bool MoveLeft(Key key, KeyModifiers modifiers)
        {
            return (key == Key.Tab && (modifiers & KeyModifiers.Shift) != 0) ||
                   (key == Key.Left && (modifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0);
        }

        private bool MoveRight(Key key, KeyModifiers modifiers)
        {
            return (key == Key.Tab && (modifiers & KeyModifiers.Shift) == 0) ||
                   (key == Key.Right && (modifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0);
        }

        private bool MoveDown(Key key, KeyModifiers modifiers)
        {
            return key == Key.Down;
        }
        
        private bool MoveUp(Key key, KeyModifiers modifiers)
        {
            return key == Key.Up;
        }
        
        private void OpenForEditing()
        {
            if (opened || isReadOnly)
                return;
            
            opened = true;
            Opacity = 0;
            textBox = new TextBox()
            {
                FontFamily = GetValue(FontFamilyProperty)
            };
            textBox.Classes.Add("GridViewPlainTextBox");
            textBox.DataContext = this;
            textBox.Bind(TextBox.TextProperty, new Binding("Value", BindingMode.OneTime));
            textBox.MinWidth = 0;
            textBox.Margin = new Thickness(partText?.Margin.Left ?? 0,partText?.Margin.Top ?? 0, 0, 0);
            textBox.Padding = new Thickness(partText?.Padding.Left ?? 0,partText?.Padding.Top ?? 0, 0, 0);
            var disposable1 = textBox.AddDisposableHandler(KeyDownEvent, (sender, args) =>
            {
                if (MoveLeft(args.Key, args.KeyModifiers) || MoveRight(args.Key, args.KeyModifiers))
                {
                    var wrapper = this.GetVisualParent() as IControl;
                    var itemsPresenter = wrapper?.VisualParent?.VisualParent as ItemsPresenter;
                    if (wrapper == null || itemsPresenter == null)
                        return;

                    var index = itemsPresenter.ItemContainerGenerator.IndexFromContainer(wrapper);

                    if (MoveLeft(args.Key, args.KeyModifiers))
                        index--;
                    else
                        index++;
                    
                    var next = itemsPresenter.ItemContainerGenerator.ContainerFromIndex(index)?.VisualChildren[0] as FastCellView;

                    if (next == null)
                        return;
                    
                    EndEditing();
                    next.OpenForEditing();
                    args.Handled = true;
                }

                if (MoveUp(args.Key, args.KeyModifiers) || MoveDown(args.Key, args.KeyModifiers))
                {
                    var wrapper = this.GetVisualParent() as IControl;
                    var itemsPresenter = wrapper?.VisualParent?.VisualParent as ItemsPresenter;
                    var row = itemsPresenter?.GetVisualParent()?.VisualParent as IControl;
                    var rows = row?.VisualParent?.VisualParent as ItemsPresenter;
                    if (wrapper == null || itemsPresenter == null || row == null || rows == null)
                        return;
                    
                    var innerIndex = itemsPresenter.ItemContainerGenerator.IndexFromContainer(wrapper);
                    var rowIndex = rows.ItemContainerGenerator.IndexFromContainer(row);

                    if (MoveDown(args.Key, args.KeyModifiers))
                        rowIndex++;
                    else
                        rowIndex--;

                    var newRow = rows.ItemContainerGenerator.ContainerFromIndex(rowIndex)
                        ?.VisualChildren[0]?.VisualChildren[0] as ItemsPresenter;

                    if (newRow == null)
                        return;

                    var newCell = newRow.ItemContainerGenerator.ContainerFromIndex(innerIndex)?.VisualChildren[0] as FastCellView;

                    if (newCell == null)
                        return;
                    
                    this.EndEditing();
                    newCell.OpenForEditing();
                    args.Handled = true;
                }
                if (args.Key is Key.Return or Key.Enter)
                {
                    EndEditing();
                    args.Handled = true;
                }
                else if (args.Key == Key.Escape)
                {
                    EndEditing(false);
                    args.Handled = true;
                }
            });
            var disposable2 = textBox.AddDisposableHandler(LostFocusEvent, (sender, args) =>
            {
                EndEditing();
            });
            textBoxDisposable = new CompositeDisposable(disposable1, disposable2);
            adornerLayer = AdornerLayer.GetAdornerLayer(this);
            if (adornerLayer == null)
            {
                EndEditing();
                return;
            }
            
            adornerLayer.Children.Add(textBox);
            AdornerLayer.SetAdornedElement(textBox, this);
            textBox.Focus();
            textBox.SelectAll();
            
            var toplevel = this.GetVisualRoot() as TopLevel;
            if (toplevel != null)
            {
                subscriptionsOnOpen = toplevel.AddDisposableHandler(PointerPressedEvent, (s, ev) =>
                {
                    bool hitTextbox = false;
                    ILogical? logical = ev.Source as ILogical;
                    while (logical != null)
                    {
                        if (logical == textBox)
                        {
                            hitTextbox = true;
                            break;
                        }
                        logical = logical.LogicalParent;
                    }
                    if (!hitTextbox)
                        EndEditing();
                }, RoutingStrategies.Tunnel);
            }
        }
    }
}