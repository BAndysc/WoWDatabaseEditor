using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Markup.Xaml;
using Prism.Commands;

namespace WDE.DatabaseEditors.Avalonia.Views
{
    public class TemplateDbTableEditorView : UserControl
    {
        public TemplateDbTableEditorView()
        {
            InitializeComponent();
            KeyBindings.Add(new KeyBinding()
            {
                Command = new DelegateCommand(() =>
                {
                    TextBox tb = this.FindControl<TextBox>("SearchTextBox") as TextBox;
                    tb?.Focus();
                }),
                Gesture = new KeyGesture(Key.F, AvaloniaLocator.Current
                    .GetService<PlatformHotkeyConfiguration>()?.CommandModifiers ?? KeyModifiers.Control)
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}