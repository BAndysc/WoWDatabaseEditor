using Avalonia;
using Avalonia.Controls;
using System;
using System.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace WDE.Common.Avalonia.Utils
{
    public class FocusUtils
    {
        public static readonly AvaloniaProperty<bool> FocusFirstProperty =
            AvaloniaProperty.RegisterAttached<FocusUtils, Control, bool>("FocusFirst");
        
        public static bool GetFocusFirst(Control control) => (bool?)control.GetValue(FocusFirstProperty) ?? false;
        public static void SetFocusFirst(Control control, bool value)
        {
            control.SetValue(FocusFirstProperty, value);
        }
        
        public static readonly AvaloniaProperty<NavigationMethod> FocusFirstMethodProperty =
            AvaloniaProperty.RegisterAttached<FocusUtils, Control, NavigationMethod>("FocusFirstMethod", NavigationMethod.Tab);
        
        public static NavigationMethod GetFocusFirstMethod(Control control) => (NavigationMethod?)control.GetValue(FocusFirstMethodProperty) ?? NavigationMethod.Tab;
        public static void SetFocusFirstMethod(Control control, NavigationMethod value)
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

            var thisAsControl = visual as Control;
            if (thisAsControl == null)
                return false;

            if (visual.Focusable)
            {
                visual.Focus(GetFocusFirstMethod((Control)visual));
                if (visual is TextBox tb)
                {
                    tb.SelectAll();
                }
                return true;
            }
            else
            {
                var navigationMethod = KeyboardNavigation.GetTabNavigation(thisAsControl);
                if (navigationMethod == KeyboardNavigationMode.Local)
                {
                    foreach (var item in thisAsControl.GetVisualChildren().OrderBy(
                                 child => KeyboardNavigation.GetTabIndex((IInputElement)child)))
                    {
                        if (item is IInputElement elem)
                        {
                            if (FocusFirstFocusableChild(elem))
                                return true;
                        }
                    }
                }
                else
                {
                    foreach (var item in thisAsControl.GetVisualChildren())
                    {
                        if (item is IInputElement elem)
                        {
                            if (FocusFirstFocusableChild(elem))
                                return true;
                        }
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
