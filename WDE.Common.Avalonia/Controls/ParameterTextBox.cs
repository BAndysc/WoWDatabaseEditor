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