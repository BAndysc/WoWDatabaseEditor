using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Platform;
using Avalonia.Styling;

namespace AvaloniaStyles.Controls
{
    public class BaseMessageBoxWindow : Window, IStyleable
    {
        public static readonly StyledProperty<string> MessageProperty =
            AvaloniaProperty.Register<BaseMessageBoxWindow, string>(nameof(Message));
        
        public string Message
        {
            get => GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }
        
        public static readonly StyledProperty<string> HeaderProperty =
            AvaloniaProperty.Register<BaseMessageBoxWindow, string>(nameof(Header));
        
        public string Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }
        
        public static readonly StyledProperty<IControl> ImageProperty =
            AvaloniaProperty.Register<BaseMessageBoxWindow, IControl>(nameof(Image));
        
        public IControl Image
        {
            get => GetValue(ImageProperty);
            set => SetValue(ImageProperty, value);
        }
        
        public BaseMessageBoxWindow()
        {
            this.AttachDevTools();
        }
        
        Type IStyleable.StyleKey => typeof(BaseMessageBoxWindow);

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;
        }
    }
}