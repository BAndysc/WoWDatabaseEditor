using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Prism.Commands;

namespace WDE.Common.Avalonia.Components;

public class EditableTextBlock : TemplatedControl
{
    private AdornerLayer? adornerLayer = null;
    private TextBox? textBox = null;
    private Panel panel = null!;
    private TextBlock textBlock = null!;

    private IDisposable? clickDisposable = null;
    
    private string text = "";
    public static readonly DirectProperty<EditableTextBlock, string> TextProperty = AvaloniaProperty.RegisterDirect<EditableTextBlock, string>(nameof(Text), o => o.Text, (o, v) => o.Text = v, defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<bool> UseAdornerProperty = AvaloniaProperty.Register<EditableTextBlock, bool>(nameof(UseAdorner), defaultValue: true);
    public static readonly StyledProperty<bool> SinglePressToEditProperty = AvaloniaProperty.Register<EditableTextBlock, bool>(nameof(SinglePressToEdit));
    public static readonly StyledProperty<bool> IsReadOnlyProperty = AvaloniaProperty.Register<EditableTextBlock, bool>(nameof(IsReadOnly));

    public string Text
    {
        get => text;
        set => SetAndRaise(TextProperty, ref text, value);
    }

    public bool UseAdorner
    {
        get => (bool)GetValue(UseAdornerProperty);
        set => SetValue(UseAdornerProperty, value);
    }

    public bool SinglePressToEdit
    {
        get => (bool)GetValue(SinglePressToEditProperty);
        set => SetValue(SinglePressToEditProperty, value);
    }

    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        panel = e.NameScope.Get<Panel>("PART_Panel");
        textBlock = e.NameScope.Get<TextBlock>("PART_TextBlock");
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (textBox != null)
            return;
        if (e.ClickCount == 2 || SinglePressToEdit)
        {
            SpawnTextBox();
            e.Handled = true;
        }
        else
            base.OnPointerPressed(e);
    }

    public void BeginEditing(string overrideText)
    {
        SpawnTextBox(overrideText);
    }

    private void SpawnTextBox(string? overrideText = null)
    {
        textBox = new TextBox()
        {
        };
        textBox.Padding = textBlock.Padding;
        textBox.Margin = textBox.Margin;
        textBox.BorderThickness = new Thickness(0);
        textBox.MinHeight = 0;
        textBox.Text = overrideText ?? text;
        textBox.FontFamily = textBlock.FontFamily;
        textBox.MinWidth = 0;
        textBox.IsReadOnly = IsReadOnly;
        textBox.KeyBindings.Add(new KeyBinding()
        {
            Gesture = new KeyGesture(Key.Enter),
            Command = new DelegateCommand(() => DespawnTextBox(true))
        });
        textBox.KeyBindings.Add(new KeyBinding()
        {
            Gesture = new KeyGesture(Key.Escape),
            Command = new DelegateCommand(() => DespawnTextBox(false))
        });
        textBox.LostFocus += TextBox_LostFocus;

        if (UseAdorner)
        {
            adornerLayer = AdornerLayer.GetAdornerLayer(this);
            if (adornerLayer == null)
            {
                DespawnTextBox(false);
                return;
            }
            AdornerLayer.SetAdornedElement(textBox, this);
            adornerLayer.Children.Add(textBox);
        }
        else
        {
            panel.Children.Add(textBox);
        }

        DispatcherTimer.RunOnce(() => textBox.Focus(), TimeSpan.FromMilliseconds(1));
        if (overrideText == null)
            textBox.SelectAll();
        else
            textBox.SelectionStart = textBox.SelectionEnd = textBox.Text.Length;

        if (this.GetVisualRoot() is TopLevel toplevel)
        {
            clickDisposable = toplevel.AddDisposableHandler(PointerPressedEvent, (s, ev) =>
            {
                bool hitTextbox = false;
                ILogical? logical = ev.Source as ILogical;
                while (logical != null)
                {
                    if (ReferenceEquals(logical, textBox))
                    {
                        hitTextbox = true;
                        break;
                    }
                    logical = logical.LogicalParent;
                }
                if (!hitTextbox)
                    DespawnTextBox(true);
            }, RoutingStrategies.Tunnel);
        }
    }

    private void TextBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (textBox != null)
        {
            DespawnTextBox(false);
        }
    }

    private void DespawnTextBox(bool save)
    {
        if (textBox == null)
            return;
        textBox.LostFocus -= TextBox_LostFocus;
        if (UseAdorner)
        {
            adornerLayer?.Children.Remove(textBox);
            adornerLayer = null;
        }
        else
        {
            panel.Children.Remove(textBox);
        }

        clickDisposable?.Dispose();
        clickDisposable = null;
        
        if (save)
            Text = textBox.Text ?? "";
        textBox = null;
    }
}
