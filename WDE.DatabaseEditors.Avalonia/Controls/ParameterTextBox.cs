using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Styling;
using WDE.Common.Avalonia.Controls;

namespace WDE.DatabaseEditors.Avalonia.Controls
{
    public class ParameterTextBox : FixedTextBox
    {
        private DateTime lastFocusTime;
        
        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);
            lastFocusTime = DateTime.Now;
            SelectAll();
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            if ((DateTime.Now - lastFocusTime).TotalMilliseconds < 500)
                return;
            
            base.OnPointerPressed(e);
        }

        protected override Type StyleKeyOverride => typeof(TextBox);
    }
}