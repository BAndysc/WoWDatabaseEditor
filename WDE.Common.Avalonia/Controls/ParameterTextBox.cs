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
    public class ParameterTextBox : FixedTextBox, IStyleable
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

        private bool justHandledS = false;
        protected override void OnKeyDown(KeyEventArgs e)
        {
            justHandledS = false;
            
            // S multiplies the value by 1000
            if (e.Key == Key.S &&
                e.KeyModifiers == KeyModifiers.None &&
                float.TryParse(Text, out var textAsFloat) &&
                SelectionStart == Text.Length)
            {
                Text = ((long)(textAsFloat * 1000)).ToString();
                SelectionStart = SelectionEnd = Text.Length;
                e.Handled = true;
                justHandledS = true;
            }
            // ctrl + num: set value to TAG + num
            else if (e.Key >= Key.D0 && e.Key <= Key.D9  &&
                     e.KeyModifiers is KeyModifiers.Control or KeyModifiers.Meta)
            {
                var num = (int)(e.Key - Key.D0);
                if (Tag != null && long.TryParse(Tag.ToString(), out var tagNumber))
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
                    nextTextBox.SelectionStart = nextTextBox.SelectionEnd = nextTextBox.Text.Length;
                    e.Handled = true;
                }
            }
            else
                base.OnKeyDown(e);
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            if (justHandledS &&
                (e.Text?.Equals("s", StringComparison.OrdinalIgnoreCase) ?? false))
            {
                e.Handled = true;
            }
            else
                base.OnTextInput(e);
        }

        private async void PasteAsync()
        {
            var text = await ((IClipboard)AvaloniaLocator.Current.GetService(typeof(IClipboard))).GetTextAsync();

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

        private static T? FindNext<T>(IVisual? start) where T : class, IVisual 
        {
            if (start == null)
                return null;

            var parent = start.GetVisualParent();
            if (parent == null)
                return null;

            int startChildrenIndex = 0;
            while (startChildrenIndex < parent.VisualChildren.Count &&
                   parent.VisualChildren[startChildrenIndex] != start)
                startChildrenIndex++;
            
            startChildrenIndex++;
            
            for (int i = startChildrenIndex; i < parent.VisualChildren.Count; ++i)
            {
                var find = parent.VisualChildren[i].FindDescendantOfType<T>(true);
                if (find != null)
                    return find;
            }

            return FindNext<T>(parent);
        }
        
        private static T? FindPrev<T>(IVisual? start) where T : class, IVisual 
        {
            if (start == null)
                return null;

            var parent = start.GetVisualParent();
            if (parent == null)
                return null;

            int startChildrenIndex = parent.VisualChildren.Count - 1;
            while (startChildrenIndex >= 0 &&
                   parent.VisualChildren[startChildrenIndex] != start)
                startChildrenIndex--;
            
            startChildrenIndex--;
            
            for (int i = startChildrenIndex; i >= 0 ; --i)
            {
                var find = parent.VisualChildren[i].FindDescendantOfType<T>(true);
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

        Type IStyleable.StyleKey => typeof(TextBox);

        public bool SpecialCopying
        {
            get { return specialCopying; }
            set { SetAndRaise(SpecialCopyingProperty, ref specialCopying, value); }
        }
    }
}