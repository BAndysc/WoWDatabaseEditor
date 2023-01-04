using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace WDE.Common.Avalonia.Controls
{
    /***
     * This is KeyBinding that forwards the gesture to the focused TextBox first
     */
    public class BetterKeyBinding : KeyBinding, ICommand
    {
        public static readonly StyledProperty<ICommand> CustomCommandProperty = AvaloniaProperty.Register<KeyBinding, ICommand>(nameof (CustomCommand));

        public ICommand CustomCommand
        {
            get => GetValue(CustomCommandProperty);
            set => SetValue(CustomCommandProperty, value);
        }
        
        public BetterKeyBinding()
        {
            Command = this;
        }

        public bool CanExecute(object? parameter)
        {
            // @fixme avalonia 11
            // if (FocusManager.Instance.Current is TextBox tb)
            //     return true;
            return CustomCommand.CanExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            // @fixme avalonia 11
            // if (FocusManager.Instance?.Current is TextBox tb)
            // {
            //     var ev = new KeyEventArgs()
            //     {
            //         Key = Gesture.Key,
            //         RoutedEvent = InputElement.KeyDownEvent,
            //         KeyModifiers = Gesture.KeyModifiers
            //     };
            //     tb.RaiseEvent(ev);
            //     if (!ev.Handled && CanExecute(parameter))
            //         CustomCommand.Execute(parameter);
            // }
            // else
                CustomCommand.Execute(parameter);
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CustomCommand.CanExecuteChanged += value;
            remove => CustomCommand.CanExecuteChanged -= value;
        }
    }
}
