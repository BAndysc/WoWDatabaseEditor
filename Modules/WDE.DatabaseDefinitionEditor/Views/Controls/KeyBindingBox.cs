using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace WDE.DatabaseDefinitionEditor.Views.Controls;

public class TextBoxWithNoInput : TextBox
{
    protected override Type StyleKeyOverride => typeof(TextBox);
    
    protected override void OnTextInput(TextInputEventArgs e)
    {
        // base.OnTextInput(e);
        // no handle on purpose!
    }
}

public class KeyBindingBox : TemplatedControl
{
    private TextBoxWithNoInput? textBox;
    private IDisposable? keyDisposable;
    public static readonly StyledProperty<KeyGesture?> KeyGestureProperty = AvaloniaProperty.Register<KeyBindingBox, KeyGesture?>(nameof(KeyGesture),
        defaultBindingMode: BindingMode.TwoWay);

    static KeyBindingBox()
    {
        KeyGestureProperty.Changed.AddClassHandler<KeyBindingBox>((box, e) =>
        {
            if (box.textBox != null)
                box.textBox.Text = box.KeyGesture?.ToString();
        });
    }
    
    public KeyGesture? KeyGesture
    {
        get => GetValue(KeyGestureProperty);
        set => SetValue(KeyGestureProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        textBox = e.NameScope.Get<TextBoxWithNoInput>("PART_TextBox");
        textBox.GotFocus += TextBoxOnGotFocus;
        textBox.LostFocus += TextBoxOnLostFocus;
    }

    private void TextBoxOnLostFocus(object? sender, RoutedEventArgs e)
    {
        keyDisposable?.Dispose();
        keyDisposable = null;
    }

    private void TextBoxOnGotFocus(object? sender, GotFocusEventArgs e)
    {
        var root = (this.GetVisualRoot() as InputElement);
        keyDisposable = root!.AddDisposableHandler(KeyDownEvent, Handler, RoutingStrategies.Tunnel);
    }

    private void Handler(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            keyDisposable?.Dispose();
            keyDisposable = null;
            return;
        }
        else if (e.Key == Key.Tab)
        {
            return;
        }

        var modifier = e.KeyModifiers;
        if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            modifier &= ~KeyModifiers.Shift;
        else if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
            modifier &= ~KeyModifiers.Control;
        else if (e.Key == Key.LeftAlt || e.Key == Key.RightAlt)
            modifier &= ~KeyModifiers.Alt;
        
        SetCurrentValue(KeyGestureProperty, new KeyGesture(e.Key, modifier));
        e.Handled = true;
    }
}