using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Styling;

namespace WDE.Common.Avalonia.Controls;

public class CommandTextBlock : TextBlock
{
    public static readonly StyledProperty<object?> CommandParameterProperty = AvaloniaProperty.Register<CommandTextBlock, object?>("CommandParameter");
    public static readonly StyledProperty<ICommand?> CommandProperty = AvaloniaProperty.Register<CommandTextBlock, ICommand?>("Command");
    protected override Type StyleKeyOverride => typeof(TextBlock);

    public object? CommandParameter
    {
        get => (object?)GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (Command != null)
        {
            Command.Execute(CommandParameter);
            e.Handled = true;
        }
        else
            base.OnPointerPressed(e);
    }
}