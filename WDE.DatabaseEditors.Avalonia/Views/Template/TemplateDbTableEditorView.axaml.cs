using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Prism.Commands;

namespace WDE.DatabaseEditors.Avalonia.Views.Template
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

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
        }

        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromLogicalTree(e);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
        }

        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e);
        }
    }
}