using System;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Styling;
using Avalonia.VisualTree;
using WDE.Common.Utils;

namespace WDE.Common.Avalonia.Controls
{
    public class ParameterTextBox : FixedTextBox
    {
        private DateTime lastFocusTime;
        private bool specialCopying;
        public static readonly DirectProperty<ParameterTextBox, bool> SpecialCopyingProperty = AvaloniaProperty.RegisterDirect<ParameterTextBox, bool>("SpecialCopying", o => o.SpecialCopying, (o, v) => o.SpecialCopying = v);

        public override void CustomPaste()
        {
            if (specialCopying)
                PasteAsync();
            else
                base.CustomPaste();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            // S multiplies the value by 1000
            if (e.Key == Key.S &&
                e.KeyModifiers == KeyModifiers.None &&
                float.TryParse(Text, out var textAsFloat) &&
                SelectionStart == Text.Length)
            {
                Text = ((long)(textAsFloat * 1000)).ToString();
                SelectionStart = SelectionEnd = Text.Length;
                e.Handled = true;
            }
            // ctrl + num: set value to TAG + num
            else if (
                (
                    (e.Key >= Key.D0 && e.Key <= Key.D9) || 
                    (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)
                ) && 
                e.KeyModifiers is KeyModifiers.Control or KeyModifiers.Meta)
            {
                int num = -1;
                if (e.Key >= Key.D0 && e.Key <= Key.D9)
                    num = (int)(e.Key - Key.D0);
                else if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)
                    num = (int)(e.Key - Key.NumPad0);

                if (num != -1 && Tag != null && long.TryParse(Tag.ToString(), out var tagNumber))
                {
                    Text = (tagNumber * 100 + num).ToString();
                    SelectionStart = SelectionEnd = Text.Length;
                    e.Handled = true;
                }
            }
            // ctrl up down - traverse through textboxes
            else if (e.Key is Key.Down or Key.Up &&
                     e.KeyModifiers is KeyModifiers.Control or KeyModifiers.Meta)
            {
                var nextTextBox = e.Key is Key.Down ? FindNext<ParameterTextBox>(this) : FindPrev<ParameterTextBox>(this);
                if (nextTextBox != null)
                {
                    nextTextBox.Text = Text;
                    nextTextBox.Focus();
                    nextTextBox.SelectionStart = nextTextBox.SelectionEnd = (nextTextBox.Text?.Length ?? 0);
                    e.Handled = true;
                }
            }
            else
                base.OnKeyDown(e);
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            if ((e.Text?.Equals("s", StringComparison.OrdinalIgnoreCase) ?? false) && float.TryParse(Text, out var textAsFloat) &&
                SelectionStart == Text.Length)
            {
                Text = ((long)(textAsFloat * 1000)).ToString();
                SelectionStart = SelectionEnd = Text.Length;
                e.Handled = true;
            }
            else
                base.OnTextInput(e);
        }

        private async void PasteAsync()
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            var text = clipboard == null ? null : await clipboard.GetTextAsync();

            if (text is null) 
                return;
            
            var coords = CoordsParser.ExtractCoords(text);
            if (coords.HasValue)
            {
                var nextTextBox = FindNext<ParameterTextBox>(this);
                var nextNextTextBox = FindNext<ParameterTextBox>(nextTextBox);
                var nextNextNextTextBox = FindNext<ParameterTextBox>(nextNextTextBox);

                this.Text = coords.Value.x.ToString(CultureInfo.InvariantCulture);
                if (nextTextBox != null)
                    nextTextBox.Text = coords.Value.y.ToString(CultureInfo.InvariantCulture);
                if (nextNextTextBox != null)
                    nextNextTextBox.Text = coords.Value.z.ToString(CultureInfo.InvariantCulture);
                if (nextNextNextTextBox != null && coords.Value.o.HasValue)
                    nextNextNextTextBox.Text = coords.Value.o.Value.ToString(CultureInfo.InvariantCulture);
            }
            else
                Paste();
        }

        private static T? FindNext<T>(Visual? start) where T : Visual
        {
            if (start == null)
                return null;

            var parent = start.GetVisualParent();
            if (parent == null)
                return null;

            int startChildrenIndex = 0;
            var visualChildren = parent.GetVisualChildren().ToList();
            while (startChildrenIndex < visualChildren.Count &&
                   !ReferenceEquals(visualChildren[startChildrenIndex], start))
                startChildrenIndex++;
            
            startChildrenIndex++;
            
            for (int i = startChildrenIndex; i < visualChildren.Count; ++i)
            {
                var find = visualChildren[i].FindDescendantOfType<T>(true);
                if (find != null)
                    return find;
            }

            return FindNext<T>(parent);
        }
        
        private static T? FindPrev<T>(Visual? start) where T : Visual 
        {
            if (start == null)
                return null;

            var parent = start.GetVisualParent();
            if (parent == null)
                return null;

            var visualChildren = parent.GetVisualChildren().ToList();
            int startChildrenIndex = visualChildren.Count - 1;
            while (startChildrenIndex >= 0 &&
                   !ReferenceEquals(visualChildren[startChildrenIndex], start))
                startChildrenIndex--;
            
            startChildrenIndex--;
            
            for (int i = startChildrenIndex; i >= 0 ; --i)
            {
                var find = visualChildren[i].FindDescendantOfType<T>(true);
                if (find != null)
                    return find;
            }

            return FindPrev<T>(parent);
        }
        
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

        public bool SpecialCopying
        {
            get { return specialCopying; }
            set { SetAndRaise(SpecialCopyingProperty, ref specialCopying, value); }
        }
    }
}
