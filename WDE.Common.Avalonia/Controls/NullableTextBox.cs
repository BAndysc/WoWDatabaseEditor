using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Metadata;

namespace WDE.Common.Avalonia.Controls;

public class NullableTextBox : TemplatedControl
{
    private string lastNonNullText = "";
    
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<NullableTextBox, string?>("Text", null, false, BindingMode.TwoWay);

    public static readonly StyledProperty<string?> WatermarkProperty =
        AvaloniaProperty.Register<NullableTextBox, string?>(nameof(Watermark));
    
    public static readonly StyledProperty<bool> IsNotNullProperty = AvaloniaProperty.Register<NullableTextBox, bool>(nameof(IsNotNull));

    static NullableTextBox()
    {
        TextProperty.Changed.AddClassHandler<NullableTextBox>((x, _) => x.UpdateIsNull());
        IsNotNullProperty.Changed.AddClassHandler<NullableTextBox>((x, _) => x.UpdateText());
    }

    private bool inEvent = false;
    
    private void UpdateText()
    {
        if (inEvent)
            return;
        
        inEvent = true;
        if (IsNotNull && Text == null)
        {
            SetCurrentValue(TextProperty, lastNonNullText);
        }
        else if (!IsNotNull)
        {
            lastNonNullText = Text ?? "";
            SetCurrentValue(TextProperty, null);
        }
        inEvent = false;
    }

    private void UpdateIsNull()
    {
        if (inEvent)
            return;
        
        inEvent = true;
        SetCurrentValue(IsNotNullProperty, Text != null);
        inEvent = false;
    }

    [Content]
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string? Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }
    
    public bool IsNotNull
    {
        get => GetValue(IsNotNullProperty);
        set => SetValue(IsNotNullProperty, value);
    }
}