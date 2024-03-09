using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Prism.Commands;

namespace WDE.CommonViews.Avalonia.Parameters;

public partial class StringPickerView : UserControl
{
    public StringPickerView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        var tb = this.FindControl<TextBox>("TextBox") ?? throw new NullReferenceException("StringPickerView's TEMPLATE TextBox not found");
        tb.KeyBindings.Add(new KeyBinding()
        {
            Gesture = new(Key.Enter),
            Command = new DelegateCommand(() =>
            {
                var args = new TextInputEventArgs();
                args.RoutedEvent = TextInputEvent;
                args.Source = tb;
                args.Text = "\n";
                tb.RaiseEvent(args);
            })
        });
    }
}