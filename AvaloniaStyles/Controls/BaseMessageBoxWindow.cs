using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using AvaloniaStyles.Utils;
using Classic.Avalonia.Theme;

namespace AvaloniaStyles.Controls
{
    public class BaseMessageBoxWindow : ClassicWindow
    {
        protected override Type StyleKeyOverride => typeof(BaseMessageBoxWindow);

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

            if (TryGetPlatformHandle() is { } handle)
               Win32.SetDarkMode(handle.Handle, SystemTheme.EffectiveThemeIsDark);
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;
            if (Background is ISolidColorBrush brush)
                if (TryGetPlatformHandle() is { } handle)
                    Win32.SetTitleBarColor(handle.Handle, brush.Color);
        }
    }
}