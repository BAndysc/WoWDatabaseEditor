using Avalonia;
using Avalonia.Controls;

namespace WDE.Conditions.Avalonia.Views
{
    public class LabeledControl : ContentControl
    {
        private string? header;
        public static readonly DirectProperty<LabeledControl, string?> HeaderProperty = AvaloniaProperty.RegisterDirect<LabeledControl, string?>(nameof(Header), o => o.Header, (o, v) => o.Header = v);

        public string? Header
        {
            get => header;
            set => SetAndRaise(HeaderProperty, ref header, value);
        }
    }
}