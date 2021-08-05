using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Styling;

namespace WDE.Common.Avalonia.Controls
{
    public class ParameterTextBox : FixedTextBox, IStyleable
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

        Type IStyleable.StyleKey => typeof(TextBox);
    }
}