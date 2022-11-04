using System.ComponentModel;

namespace WDE.Common.Settings;

public class BoolGenericSetting : IBoolGenericSetting
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public string Name { get; }
    public string? Help { get; }
    private bool value;

    public bool Value
    {
        get => value;
        set
        {
            if (this.value == value)
                return;

            this.value = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        }
    }

    public BoolGenericSetting(string name, bool defaultValue, string? help = null)
    {
        Name = name;
        value = defaultValue;
        Help = help;
    }
}