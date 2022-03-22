using System.Windows.Input;
using WDE.Common.Types;

namespace WDE.Common.QuickAccess;

public struct QuickAccessItem
{
    public QuickAccessItem(ImageUri icon, string text, string action, string description, ICommand command, object? parameter, byte score = 50)
    {
        Icon = icon;
        Text = text;
        Action = action;
        Description = description;
        Command = command;
        Parameter = parameter;
        Score = score;
    }

    public ImageUri Icon { get; }
    public string Text { get; }
    public string Action { get; }
    public string Description { get; }
    public ICommand Command { get; }
    public object? Parameter { get; }
    public byte Score { get; }
}