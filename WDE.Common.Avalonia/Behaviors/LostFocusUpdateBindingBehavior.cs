using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;
using WDE.MVVM.Observable;

namespace WDE.Common.Avalonia.Behaviors;

public class LostFocusUpdateBindingBehavior : Behavior<TextBox>
{
    static LostFocusUpdateBindingBehavior()
    {
        TextProperty.Changed.SubscribeAction(e =>
        {
            ((LostFocusUpdateBindingBehavior) e.Sender).OnBindingValueChanged();
        });
    }
    
    public static readonly StyledProperty<string?> TextProperty = AvaloniaProperty.Register<LostFocusUpdateBindingBehavior, string?>(
        nameof(Text), defaultBindingMode: BindingMode.TwoWay);

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    protected override void OnAttached()
    {
        if (AssociatedObject != null)
            AssociatedObject.LostFocus += OnLostFocus;
        base.OnAttached();
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject != null)
            AssociatedObject.LostFocus -= OnLostFocus;
        base.OnDetaching();
    }
        
    private void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (AssociatedObject != null)
            SetCurrentValue(TextProperty, AssociatedObject.Text);
    }
        
    private void OnBindingValueChanged()
    {
        if (AssociatedObject != null)
            AssociatedObject.SetCurrentValue(TextBox.TextProperty, Text);
    }
}


public class NumericUpDownLostFocusUpdateBindingBehavior : Behavior<NumericUpDown>
{
    static NumericUpDownLostFocusUpdateBindingBehavior()
    {
        ValueProperty.Changed.SubscribeAction(e =>
        {
            ((NumericUpDownLostFocusUpdateBindingBehavior) e.Sender).OnBindingValueChanged();
        });
    }
    
    public static readonly StyledProperty<decimal?> ValueProperty = AvaloniaProperty.Register<NumericUpDownLostFocusUpdateBindingBehavior, decimal?>(
        nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public decimal? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    protected override void OnAttached()
    {
        if (AssociatedObject != null)
            AssociatedObject.LostFocus += OnLostFocus;
        base.OnAttached();
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject != null)
            AssociatedObject.LostFocus -= OnLostFocus;
        base.OnDetaching();
    }
        
    private void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (AssociatedObject != null)
            SetCurrentValue(ValueProperty, AssociatedObject.Value);
    }
        
    private void OnBindingValueChanged()
    {
        if (AssociatedObject != null)
            AssociatedObject.SetCurrentValue(NumericUpDown.ValueProperty, Value);
    }
}