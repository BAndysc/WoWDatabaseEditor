using System.ComponentModel;
using System.Windows.Input;

namespace WDE.Common.Settings;

public interface IButtonGenericSetting : IGenericSetting
{
    ICommand Command { get; }
    string ButtonText { get; }
}

// A settings row that just triggers an action (e.g. debug/diagnostic tools).
public class ButtonGenericSetting : IButtonGenericSetting
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public string Name { get; }
    public string? Help { get; }
    public string ButtonText { get; }
    public ICommand Command { get; }

    public ButtonGenericSetting(string name, string buttonText, ICommand command, string? help = null)
    {
        Name = name;
        ButtonText = buttonText;
        Command = command;
        Help = help;
    }
}
