using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Metadata;

namespace WDE.SmartScriptEditor.Avalonia.ExtendedTextBlock
{
    public class FormattedTextBlock : Control
    {
        private static FormattedTextDrawer drawer;
        private static int STYLE_DEFAULT = 0;
        private static int STYLE_PARAMETER = 1;
        private static int STYLE_SOURCE = 2;

        static FormattedTextBlock()
        {
            drawer = new FormattedTextDrawer();
            drawer.AddStyle(STYLE_DEFAULT, new Typeface("Open Sans"), 12, Brushes.Black);
            drawer.AddStyle(STYLE_PARAMETER, new Typeface("Fira Code", FontStyle.Normal, FontWeight.Bold), 12, Brushes.Navy);
            drawer.AddStyle(STYLE_SOURCE, new Typeface("Fira Code", FontStyle.Normal, FontWeight.Bold), 12, Brushes.ForestGreen);
            
            AffectsRender<FormattedTextBlock>(BackgroundProperty);
            AffectsRender<FormattedTextBlock>(TextProperty);
            AffectsMeasure<FormattedTextBlock>(TextProperty);
        }
        
        private int overPartIndex = -1;
        private Point currentPos;
        
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            currentPos = e.GetPosition(this);
            InvalidateVisual();
        }

        protected override void OnPointerLeave(PointerEventArgs e)
        {
            base.OnPointerLeave(e);
            currentPos = new Point(-100, -100);
            InvalidateVisual();
        }
        
        private enum State
        {
            Text,
            InTag,
            InClosingTag,
            Slash
        }

        private void ParseTest(string text, Action<string, int, bool, bool> action)
        {
            if (text == null)
                return;
            
            int partIndex = 0;
            bool one = false;
            bool parameter = false;
            bool source = false;

            State state = State.Text;

            int startIndex = 0;
            int endIndex = 0;

            for (var index = 0; index < text.Length; index++)
            {
                ReadOnlySpan<char> toFulsh = null;
                var s = text[index];
                
                if (s == '\\' && state == State.Text)
                {
                    if (index > startIndex)
                        toFulsh = text.AsSpan(startIndex, index - startIndex);
                    startIndex = index + 1;
                    state = State.Slash;
                }
                else if (state == State.Slash)
                {
                    state = State.Text;
                }
                else if (s == '[' && state == State.Text)
                {
                    if (index > startIndex)
                        toFulsh = text.AsSpan(startIndex, index - startIndex);

                    startIndex = index + 1;
                    state = State.InTag;
                }
                else if (s == '/' && state == State.InTag)
                {
                    startIndex = index + 1;
                    state = State.InClosingTag;
                }
                else if (s == ']' && state == State.InTag)
                {
                    partIndex++;

                    if (text[startIndex] == 'p')
                        parameter = true;
                    if (text[startIndex] == 's')
                        source = true;

                    startIndex = index + 1;
                    state = State.Text;
                }
                else if (s == ']' && state == State.InClosingTag)
                {
                    partIndex++;

                    if (text[startIndex] == 'p')
                        parameter = false;
                    if (text[startIndex] == 's')
                        source = false;
                    
                    startIndex = index + 1;
                    state = State.Text;
                } 
                else if (s == ' ' && state == State.Text && toFulsh == null)
                {
                    toFulsh = text.AsSpan(startIndex, index - startIndex + 1);
                    startIndex = index + 1;
                }

                if (state == State.Text && index == text.Length - 1)
                    toFulsh = text.AsSpan(startIndex);

                if (toFulsh == null)
                    continue;

                action(toFulsh.ToString(), partIndex, source, parameter);
            }
        }

        private bool wasOverLink = false;
        public override void Render(DrawingContext context)
        {
            base.Render(context);
            if (Background != null)
            {
                context.FillRectangle(Background, new Rect(Bounds.Size));
            }

            Stopwatch sw = new();
            Stopwatch sw2 = new();
            sw.Start();
            int newOverPartIndex = -1;
            double x = Padding.Left;
            double y = Padding.Top;
            bool wasWrapped = true;
            bool overLink = false;

            ParseTest(Text, (text, partIndex, source, parameter) =>
            {
                var styleId = parameter ? STYLE_PARAMETER : (source ? STYLE_SOURCE : STYLE_DEFAULT);
   
                Rect bounds;
                sw2.Start();
                (wasWrapped, bounds) = drawer.Draw(context, text, styleId, !wasWrapped, ref x, ref y, Padding.Left, Bounds.Width);
                sw2.Stop();

                if (overPartIndex == partIndex && (source || parameter) && IsPointerOver)
                {
                    overLink = true;
                    drawer.DrawUnderline(context, styleId, bounds);
                }

                if (bounds.Contains(currentPos))
                    newOverPartIndex = partIndex;
            });

            overPartIndex = newOverPartIndex;
            
            if (overLink && !wasOverLink)
                PseudoClasses.Add(":overlink");
            else if (!overLink && wasOverLink)
                PseudoClasses.Remove(":overlink");

            wasOverLink = overLink;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            Rect bounds;
            bool wasWrapped = true;
            bool everWrapped = false;
            double x = 0, y = 0;
            ParseTest(Text, (text, partIndex, source, parameter) =>
            {
                var styleId = parameter ? STYLE_PARAMETER : (source ? STYLE_SOURCE : STYLE_DEFAULT);
                (wasWrapped, bounds) = drawer.Measure(text, styleId, !wasWrapped, ref x, ref y, availableSize.Width);
                if (bounds.Contains(currentPos))
                    overPartIndex = partIndex;
                everWrapped |= wasWrapped;
            });
            var size = new Size(everWrapped ? availableSize.Width : x, y + drawer.LineHeight);
            return size.Inflate(Padding);
        }

        /// <summary>
        /// Defines the <see cref="Text"/> property.
        /// </summary>
        public static readonly DirectProperty<FormattedTextBlock, string> TextProperty =
            AvaloniaProperty.RegisterDirect<FormattedTextBlock, string>(
                nameof(Text),
                o => o.Text,
                (o, v) => o.Text = v);

        private string _text = "";
        
        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        [Content]
        public string Text
        {
            get { return _text; }
            set { SetAndRaise(TextProperty, ref _text, value); }
        }
        
        public static readonly StyledProperty<IBrush> BackgroundProperty =
            Border.BackgroundProperty.AddOwner<TextBlock>();

        /// <summary>
        /// Gets or sets a brush used to paint the control's background.
        /// </summary>
        public IBrush Background
        {
            get { return GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }
        
        /// <summary>
        /// Defines the <see cref="Padding"/> property.
        /// </summary>
        public static readonly StyledProperty<Thickness> PaddingProperty =
            Decorator.PaddingProperty.AddOwner<TextBlock>();
        
        /// <summary>
        /// Gets or sets the padding to place around the <see cref="Text"/>.
        /// </summary>
        public Thickness Padding
        {
            get { return GetValue(PaddingProperty); }
            set { SetValue(PaddingProperty, value); }
        }
    }
}