using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Styling;

namespace WDE.Common.Avalonia.Controls
{
    // normal TextBox has bug when AcceptsReturn is false
    public class FixedTextBox : TextBox, IStyleable
    {
        Type IStyleable.StyleKey => typeof(TextBox);
        
        protected override void OnTextInput(TextInputEventArgs e)
        {
            if (!e.Handled)
            {
                if (AcceptsReturn || (e.Text != "\r" && e.Text != "\n"))
                    base.OnTextInput(e);
            }
        }
    }
}