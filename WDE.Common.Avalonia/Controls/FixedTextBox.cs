using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Styling;
using Avalonia.Threading;

namespace WDE.Common.Avalonia.Controls
{
    // normal TextBox has bug when AcceptsReturn is false
    public class FixedTextBox : TextBox
    {
        protected override Type StyleKeyOverride => typeof(TextBox);
        
        protected override void OnTextInput(TextInputEventArgs e)
        {
            if (!e.Handled)
            {
                if (AcceptsReturn || (e.Text != "\r" && e.Text != "\n"))
                    base.OnTextInput(e);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (KeyGestures.Paste.Matches(e))
            {
                CustomPaste();
                e.Handled = true;
            }
            else
                base.OnKeyDown(e);
            
            if (Text == "" && DataValidationErrors.GetHasErrors(this))
            {
                Text = "0";
                SelectAll();
            }
        }

        public virtual void CustomPaste()
        {
            Paste();
        }
    }
}
