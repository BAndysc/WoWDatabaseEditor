using System.Collections.Generic;
using Avalonia;
using WDE.Common.Parameters;

namespace WDE.Common.Avalonia.Controls;

public class FlagsParameterBox : BaseParameterBox
{
    public static readonly StyledProperty<Dictionary<long, SelectOption>> FlagsProperty = AvaloniaProperty.Register<FlagsParameterBox, Dictionary<long, SelectOption>>(nameof(Flags), defaultValue: new Dictionary<long, SelectOption>());

    public Dictionary<long, SelectOption> Flags
    {
        get => GetValue(FlagsProperty);
        set => SetValue(FlagsProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ParameterProperty)
        {
            if (Parameter is IParameter<long> longParameter &&
                longParameter.Items != null)
            {
                SetCurrentValue(FlagsProperty, longParameter.Items);
            }
        }
    }
}