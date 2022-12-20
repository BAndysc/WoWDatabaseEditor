using System.Collections.Generic;
using System.ComponentModel;

namespace WDE.Common.Settings;

public class ListOptionGenericSetting : IListOptionGenericSetting
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public string Name { get;  }
    public string? Help { get; }
    public IReadOnlyList<object> Options { get; }
    private object selectedOption;
    public object SelectedOption
    {
        get => selectedOption;
        set
        {
            if (Equals(selectedOption, value))
                return;
            selectedOption = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedOption)));
        }
    }

    public ListOptionGenericSetting(string name, IReadOnlyList<object> options, object currentOption, string? help)
    {
        Name = name;
        Options = options;
        selectedOption = currentOption;
        Help = help;
    }
}