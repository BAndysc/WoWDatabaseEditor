using Avalonia;
using Avalonia.Controls;
using System;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace WDE.Common.Avalonia.Utils
{
    public class FocusUtils
    {
        public static readonly AvaloniaProperty<bool> FocusFirstProperty =
            AvaloniaProperty.RegisterAttached<FocusUtils, IControl, bool>("FocusFirst");
        
        public static bool GetFocusFirst(IControl control) => control.GetValue(FocusFirstProperty);
        public static void SetFocusFirst(IControl control, bool value)
        {
            control.SetValue(FocusFirstProperty, value);
        }
        
        public static readonly AvaloniaProperty<NavigationMethod> FocusFirstMethodProperty =
            AvaloniaProperty.RegisterAttached<FocusUtils, IControl, NavigationMethod>("FocusFirstMethod", NavigationMethod.Tab);
        
        public static NavigationMethod GetFocusFirstMethod(IControl control) => control.GetValue(FocusFirstMethodProperty);
        public static void SetFocusFirstMethod(IControl control, NavigationMethod value)
        {
            control.SetValue(FocusFirstMethodProperty, value);
        }


        static FocusUtils()
        {
            FocusFirstProperty.Changed.Subscribe(e =>
            {
                if (e.Sender is TemplatedControl tc)
                {
                    if (e.NewValue.Value)
                    {
                        FocusFirstFocusableChild(tc);
                        tc.TemplateApplied += OnTemplateApplied;
                    }
                    else
                        tc.TemplateApplied -= OnTemplateApplied;
                }
            });
        }

        public static bool FocusFirstFocusableChild(IInputElement? visual)
        {
            if (visual == null)
                return false;

            if (visual.Focusable)
            {
                FocusManager.Instance!.Focus(visual, GetFocusFirstMethod((IControl)visual));
                if (visual is TextBox tb)
                {
                    tb.SelectAll();
                }
                return true;
            }
            else
            {
                foreach (var item in visual.VisualChildren)
                {
                    if (item is IInputElement elem)
                    {
                        if (FocusFirstFocusableChild(elem))
                            return true;
                    }
                }
            }

            return false;
        }

        private static void OnTemplateApplied(object? sender, TemplateAppliedEventArgs e)
        {
            Dispatcher.UIThread.Post(() => FocusFirstFocusableChild(sender as IInputElement));
            if (sender is TemplatedControl tc)
            {
                tc.TemplateApplied -= OnTemplateApplied;
            }
        }
    }
}