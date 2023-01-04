using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace WDE.EventAiEditor.Avalonia.Editor.UserControls
{
    public class MiniEventIcon : TemplatedControl
    {
        private string text = "";
        public static readonly DirectProperty<MiniEventIcon, string> TextProperty = AvaloniaProperty.RegisterDirect<MiniEventIcon, string>("Text", o => o.Text, (o, v) => o.Text = v);

        public static readonly DirectProperty<MiniEventIcon, bool> IsNotEmojiProperty = AvaloniaProperty.RegisterDirect<MiniEventIcon, bool>("IsNotEmoji", o => o.IsNotEmoji);
        public bool IsNotEmoji => string.IsNullOrEmpty(Text) || Text[0] <= 127;

        public string Text
        {
            get => text;
            set
            {
                SetAndRaise(TextProperty, ref text, value);
                RaisePropertyChanged(IsNotEmojiProperty, false, IsNotEmoji);
            }
        }
    }
}