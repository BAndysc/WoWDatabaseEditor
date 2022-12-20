using System.Windows.Input;

namespace WDE.Common.Utils;

public class CommandKeyBinding
{
    public CommandKeyBinding(ICommand command, string keyGesture, bool highestPriority = false)
    {
        Command = command;
        KeyGesture = keyGesture;
        HighestPriority = highestPriority;
    }

    public ICommand Command { get; }
    public string KeyGesture { get; }
    public bool HighestPriority { get; }
}