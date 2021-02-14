using Avalonia;
using Avalonia.Controls;
using System;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Threading;

namespace WDE.Common.Avalonia.Utils
{
    public class FocusUtils
    {
        public static readonly AvaloniaProperty<bool> FocusFirstProperty =
            AvaloniaProperty.RegisterAttached<FocusUtils, IControl, bool>("FocusFirst");
        
        public static bool GetFocusFirst(IControl control) => control.GetValue(FocusFirstProperty);
        public static void SetFocusFirst(IControl control, bool value) => control.SetValue(FocusFirstProperty, value);

        static FocusUtils()
        {
            FocusFirstProperty.Changed.Subscribe(e =>
            {
                if (e.Sender is TemplatedControl tc)
                    tc.TemplateApplied += OnTemplateApplied;
            });
        }

        private static void OnTemplateApplied(object? sender, TemplateAppliedEventArgs e)
        {
            FocusManager.Instance.Focus(sender as IInputElement, NavigationMethod.Pointer);
            if (sender is TemplatedControl tc)
                tc.TemplateApplied -= OnTemplateApplied;
        }
    }
}