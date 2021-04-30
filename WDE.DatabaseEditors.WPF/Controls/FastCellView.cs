using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace WDE.DatabaseEditors.WPF.Controls
{
    public class FastCellView : FastCellViewBase
    {
        public static readonly DependencyProperty ShowChooseButtonProperty = DependencyProperty.Register("ShowChooseButton", typeof(bool), typeof(FastCellView));
        public static readonly DependencyProperty ChooseParameterCommandProperty = DependencyProperty.Register("ChooseParameterCommand", typeof(ICommand), typeof(FastCellView));

        public ICommand? ChooseParameterCommand
        {
            get => (ICommand?)GetValue(ChooseParameterCommandProperty);
            set => SetValue(ChooseParameterCommandProperty, value);
        }

        public bool ShowChooseButton
        {
            get => (bool)GetValue(ShowChooseButtonProperty);
            set => SetValue(ShowChooseButtonProperty, value);
        }
        static FastCellView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FastCellView), new FrameworkPropertyMetadata(typeof(FastCellView)));
        }

        private Grid? partPanel;
        private TextBlock? partText;
        private TextBox? textBox;
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            partPanel = GetTemplateChild("PART_Panel") as Grid;
            partText = GetTemplateChild("PART_text") as TextBlock;

            if (!IsReadOnly)
                partText!.Cursor = Cursors.IBeam;

            if (ShowChooseButton && !IsReadOnly)
            {
                var chooseButton = new Button()
                {
                    Content = "...",
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Width = 30,
                    Command = ChooseParameterCommand,
                    CommandParameter = DataContext
                };

                partPanel!.Children.Add(chooseButton);
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (IsReadOnly || e.Handled)
                return;
            e.Handled = true;
            OpenForEditing();
        }

        private bool opened;
        private void OpenForEditing()
        {
            if (opened || IsReadOnly)
                return;

            opened = true;
            textBox = new TextBox()
            {
                FontFamily = (System.Windows.Media.FontFamily)GetValue(FontFamilyProperty)
            };
            textBox.DataContext = this;
            textBox.SetValue(TextBox.TextProperty, Value.ToString());
            textBox.Margin = partText?.Margin ?? BorderThickness;
            textBox.VerticalContentAlignment = VerticalAlignment.Center;
            textBox.BorderThickness = new Thickness(0);
            textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
            textBox.LostFocus += TextBox_LostFocus;
            partPanel.Children.Add(textBox);
            textBox.Focus();
            textBox.SelectAll();

            /*var toplevel = this.GetVisualRoot() as TopLevel;
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
            }*/
        }

        private void EndEditing(bool commit = true)
        {
            textBox.PreviewKeyDown -= TextBox_PreviewKeyDown;
            textBox.LostFocus -= TextBox_LostFocus;
            if (textBox != null && commit)
            {
                if (Value is long)
                {
                    if (long.TryParse(textBox.Text, out var value))
                        Value = value;
                }
                else if (Value is float)
                {
                    if (float.TryParse(textBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                        Value = value;
                }
                else
                    Value = textBox.Text;
            }

            if (textBox != null)
                partPanel.Children.Remove(textBox);

            textBox = null;
            opened = false;
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            EndEditing();
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is Key.Return or Key.Enter)
            {
                EndEditing();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                EndEditing(false);
                e.Handled = true;
            }
        }
    }
}
