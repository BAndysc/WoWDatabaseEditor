using System.Windows.Input;
using WDE.Common.Types;

namespace WDE.Common.QuickAccess;

public struct QuickAccessItem
{
    public QuickAccessItem(ImageUri icon, string text, string action, string description, ICommand command, object? parameter)
    {
        Icon = icon;
        Text = text;
        Action = action;
        Description = description;
        Command = command;
        Parameter = parameter;
    }

    public ImageUri Icon { get; }
    public string Text { get; }
    public string Action { get; }
    public string Description { get; }
    public ICommand Command { get; }
    public object? Parameter { get; }
}