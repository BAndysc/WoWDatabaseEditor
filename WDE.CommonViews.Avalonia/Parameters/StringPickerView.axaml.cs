using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Prism.Commands;

namespace WDE.CommonViews.Avalonia.Parameters;

public class StringPickerView : UserControl
{
    public StringPickerView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        var tb = this.FindControl<TextBox>("TextBox");
        tb.KeyBindings.Add(new KeyBinding()
        {
            Gesture = new(Key.Enter),
            Command = new DelegateCommand(() =>
            {
                tb.RaiseEvent(new TextInputEventArgs()
                {
                    RoutedEvent = TextBox.TextInputEvent,
                    Source = tb,
                    Text = "\n"
                });
            })
        });
    }
}