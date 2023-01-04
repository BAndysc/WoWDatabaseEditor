using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using WDE.Common.Avalonia.Utils;

namespace WDE.Common.Avalonia.Controls
{
    /***
     * This is KeyBinding that forwards the gesture to the focused TextBox first
     */
    public class BetterKeyBinding : KeyBinding, ICommand
    {
        public static readonly StyledProperty<ICommand> CustomCommandProperty = AvaloniaProperty.Register<BetterKeyBinding, ICommand>(nameof (CustomCommand));

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
            var focusManager = Application.Current?.GetTopLevel()?.FocusManager;
            var currentAsTextBox = focusManager?.GetFocusedElement() as TextBox;
            var currentAsTextEditor = focusManager?.GetFocusedElement() as TextEditor;
            var currentAsTextArea = focusManager?.GetFocusedElement() as TextArea;
            if (currentAsTextBox != null || currentAsTextEditor != null || currentAsTextArea != null)
                 return true;
            return CustomCommand.CanExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            var focusManager = Application.Current?.GetTopLevel()?.FocusManager;
            var currentAsTextBox = focusManager?.GetFocusedElement() as TextBox;
            var currentAsTextEditor = focusManager?.GetFocusedElement() as TextEditor;
            var currentAsTextArea = focusManager?.GetFocusedElement() as TextArea;
            if (currentAsTextBox != null || currentAsTextEditor != null || currentAsTextArea != null)
            {
                var ev = new KeyEventArgs()
                {
                    Key = Gesture.Key,
                    KeyModifiers = Gesture.KeyModifiers,
                    RoutedEvent = InputElement.KeyDownEvent
                };
                ((Control?)currentAsTextBox ?? (Control?)currentAsTextEditor ?? currentAsTextArea)!.RaiseEvent(ev);
                if (!ev.Handled && CanExecute(parameter))
                    CustomCommand.Execute(parameter);
            }
            else
                CustomCommand.Execute(parameter);
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CustomCommand.CanExecuteChanged += value;
            remove => CustomCommand.CanExecuteChanged -= value;
        }
    }
}
