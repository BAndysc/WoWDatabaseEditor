using System;
using System.Runtime.InteropServices;
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
        
        public static readonly StyledProperty<Control> ImageProperty =
            AvaloniaProperty.Register<BaseMessageBoxWindow, Control>(nameof(Image));
        
        public Control Image
        {
            get => GetValue(ImageProperty);
            set => SetValue(ImageProperty, value);
        }
        
        public BaseMessageBoxWindow()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                PseudoClasses.Add(":macos");
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                PseudoClasses.Add(":windows");
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                PseudoClasses.Add(":linux");

        }
        
        Type IStyleable.StyleKey => typeof(BaseMessageBoxWindow);

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;
        }
    }
}