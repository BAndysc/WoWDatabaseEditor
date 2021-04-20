using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
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
        private ICommand? chooseParameterCommand;
        public static readonly DirectProperty<FastCellView, bool> ShowChooseButtonProperty = AvaloniaProperty.RegisterDirect<FastCellView, bool>("ShowChooseButton", o => o.ShowChooseButton, (o, v) => o.ShowChooseButton = v);
        public static readonly DirectProperty<FastCellView, ICommand?> ChooseParameterCommandProperty = AvaloniaProperty.RegisterDirect<FastCellView, ICommand?>("ChooseParameterCommand", o => o.ChooseParameterCommand, (o, v) => o.ChooseParameterCommand = v);

        public ICommand? ChooseParameterCommand
        {
            get => chooseParameterCommand;
            set => SetAndRaise(ChooseParameterCommandProperty, ref chooseParameterCommand, value);
        }
        
        public bool ShowChooseButton
        {
            get => showChooseButton;
            set
            {
                SetAndRaise(ShowChooseButtonProperty, ref showChooseButton, value);
            }
        }

        private TextBlock? partText;

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            
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
                    
                e.NameScope.Find<Panel>("PART_Panel").Children.Add(chooseButton);
            }
        }
        //
        // static FastCellView()
        // {
        //     AffectsRender<FastCellView>(IsModifiedProperty);
        // }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            if (isReadOnly)
                return;
            if (e.Source != partText)
                return;

            OpenForEditing();
        }

        private System.IDisposable? subscriptionsOnOpen;

        private void EndEditing()
        {
            Opacity = 1;
            subscriptionsOnOpen?.Dispose();
            subscriptionsOnOpen = null;
            if (textBox != null && adornerLayer != null)
                adornerLayer.Children.Remove(textBox);
            textBox = null;
            adornerLayer = null;

            opened = false;
        }
        
        private TextBox? textBox;
        private AdornerLayer? adornerLayer;
        private bool opened = false;
        
        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);
            //OpenForEditing();
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
            textBox.Bind(TextBox.TextProperty, new Binding("Value"));
            textBox.Margin = partText?.Margin ?? BorderThickness;
            textBox.LostFocus += (sender, args) =>
            {
                EndEditing();
            };
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
                    //ev.Handled = true;
                }, RoutingStrategies.Tunnel);
            }
        }
    }
}