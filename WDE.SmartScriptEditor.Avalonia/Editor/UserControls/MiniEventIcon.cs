using Avalonia;
using Avalonia.Controls.Primitives;

namespace WDE.SmartScriptEditor.Avalonia.Editor.UserControls
{
    public class MiniEventIcon : TemplatedControl
    {
        private string text = "";
        public static readonly DirectProperty<MiniEventIcon, string> TextProperty = AvaloniaProperty.RegisterDirect<MiniEventIcon, string>("Text", o => o.Text, (o, v) => o.Text = v);

        public string Text
        {
            get => text;
            set => SetAndRaise(TextProperty, ref text, value);
        }
    }
}