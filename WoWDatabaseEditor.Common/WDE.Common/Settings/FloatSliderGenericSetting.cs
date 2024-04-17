using System;
using System.ComponentModel;
using System.Windows.Input;

namespace WDE.Common.Settings;

public class FloatSliderGenericSetting : IFloatSliderGenericSetting
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public string Name { get; }
    public string? Help { get; }
    public float Min { get; }
    public float Max { get; }
    public bool WholeNumbers { get; }
    private float value;

    public FloatSliderGenericSetting(string name, float value, float min, float max, string? help = null, bool wholeNumbers = false)
    {
        Name = name;
        this.value = value;
        Min = min;
        Max = max;
        Help = help;
        WholeNumbers = wholeNumbers;
    }

    public float Value
    {
        get => value;
        set
        {
            var newValue = Math.Clamp(value, Min, Max);
            if (Math.Abs(this.value - newValue) < 0.0001f)
                return;
            
            this.value = newValue;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        }
    }
}