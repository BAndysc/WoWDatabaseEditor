using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Metadata;
using Avalonia.Threading;
using WDE.Common.Avalonia.Services;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Utils;

namespace WDE.Common.Avalonia.Controls
{
    public class FormattedTextBlock : Control
    {
        private static FormattedTextDrawer drawer = null!;
        private static int STYLE_DEFAULT = 0;
        private static int STYLE_PARAMETER = 1;
        private static int STYLE_SOURCE = 2;
        private static int STYLE_DEFAULT_SELECTED = 3;

        static FormattedTextBlock()
        {
            AffectsRender<FormattedTextBlock>(IsSelectedProperty);
            AffectsRender<FormattedTextBlock>(BackgroundProperty);
            AffectsRender<FormattedTextBlock>(TextProperty);
            AffectsMeasure<FormattedTextBlock>(TextProperty);
        }

        private static void EnsureDrawerInitialized()
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (drawer != null)
                return;
            
            // this is a hack optimization
            // this probably will be removed
            // when avalonia has support for "RUN"
            drawer = new FormattedTextDrawer();
            if (!Application.Current!.Styles.TryGetResource("MainFontSans", null, out var mainFontSans)
                || mainFontSans is not FontFamily mainFontSansFamily)
                return;
            
            if (Application.Current.Styles.TryGetResource("SmartScripts.Event.Foreground", null, out var eventColor)
                && eventColor is IBrush eventBrush)
            {
                drawer.AddStyle(STYLE_DEFAULT, new Typeface(mainFontSansFamily), 12, eventBrush);
            }
            
            if (Application.Current.Styles.TryGetResource("SmartScripts.Event.Selected.Foreground", null, out var eventSelectedColor)
                && eventSelectedColor is IBrush eventSelectedBrush)
            {
                drawer.AddStyle(STYLE_DEFAULT_SELECTED, new Typeface(mainFontSansFamily), 12, eventSelectedBrush);
            }
            
            if (Application.Current.Styles.TryGetResource("SmartScripts.Parameter.Foreground", null, out var parameterColor)
                && parameterColor is IBrush parameterBrush)
            {
                drawer.AddStyle(STYLE_PARAMETER, new Typeface("Consolas,Menlo,Courier,Courier New", FontStyle.Normal, FontWeight.Bold), 12, parameterBrush);
            }
            
            if (Application.Current.Styles.TryGetResource("SmartScripts.Source.Foreground", null, out var sourceColor)
                && sourceColor is IBrush sourceBrush)
            {
                drawer.AddStyle(STYLE_SOURCE, new Typeface("Consolas,Menlo,Courier,Courier New", FontStyle.Normal, FontWeight.Bold), 12, sourceBrush);
            }
        }

        public FormattedTextBlock()
        {
            EnsureDrawerInitialized();
        }
        
        private int overPartIndex = -1;
        private Point currentPos;
        
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            currentPos = e.GetPosition(this);
            InvalidateVisual();
        }

        protected override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);
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

        private static bool TryParseTag(ReadOnlySpan<char> text, out ReadOnlySpan<char> tag, out int? param)
        {
            tag = default;
            param = default;

            if (text.Length == 0)
                return false;
            
            if (text[0] == '[')
                text = text.Slice(1);
            if (text[^1] == ']')
                text = text.Slice(0, text.Length - 1);
            
            var indexOfSeparator = text.IndexOf('=');
            if (indexOfSeparator == -1)
            {
                tag = text;
                return true;
            }
            
            tag = text.Slice(0, indexOfSeparator);
            var paramText = text.Slice(indexOfSeparator + 1);
            if (int.TryParse(paramText, out var p))
            {
                param = p;
                return true;
            }

            return true;
        }

        private static void ParseText(Control? owner, string? text, Action<string, int, bool, bool, int, IImage?> action)
        {
            if (text == null)
                return;
            
            int partIndex = 0;
            bool parameter = false;
            bool source = false;
            IImage? drawBitmap = null;
            int contextId = -1;

            State state = State.Text;

            int startIndex = 0;

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

                    if (TryParseTag(text.AsSpan(startIndex, index - startIndex), out var tag, out var param))
                    {
                        if (tag.Equals("p", StringComparison.Ordinal))
                            parameter = true;
                        else if (tag.Equals("s", StringComparison.Ordinal))
                            source = true;
                        else if (tag.Equals("spell", StringComparison.Ordinal) &&
                                 param.HasValue)
                        {
                            var iconsDb = ViewBind.ResolveViewModel<ISpellIconDatabase>();
                            var spellId = (uint)param.Value;
                            if (!iconsDb.TryGetCached(spellId, out drawBitmap))
                            {
                                if (owner != null)
                                {
                                    async Task LoadIcon()
                                    {
                                        await ViewBind.ResolveViewModel<ISpellIconDatabase>().GetIcon(spellId);
                                        Dispatcher.UIThread.Post(() =>
                                        {
                                            owner.InvalidateVisual();
                                            owner.InvalidateMeasure();
                                        });
                                    }
                                    LoadIcon().ListenErrors();   
                                }
                            }
                        }
                        
                        if (param.HasValue)
                            contextId = param.Value;
                    }

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
                    contextId = -1;
                    
                    startIndex = index + 1;
                    state = State.Text;
                } 
                else if (s == ' ' && state == State.Text && toFulsh == null)
                {
                    toFulsh = text.AsSpan(startIndex, index - startIndex + 1);
                    if (index < text.Length - 1)
                        startIndex = index + 1;
                }
                else if ((s == '\n' || s == '\r') && state == State.Text && toFulsh == null)
                {
                    toFulsh = text.AsSpan(startIndex, index - startIndex);
                    if (index < text.Length - 1)
                        startIndex = index + 1;
                }

                if (state == State.Text && index == text.Length - 1)
                    toFulsh = text.AsSpan(startIndex).TrimEnd();

                if (toFulsh == null)
                    continue;

                action(toFulsh.ToString(), partIndex, source, parameter, contextId, drawBitmap);
                if (s == '\n' && state == State.Text)
                    action("\n", partIndex, source, parameter, contextId, null);
                drawBitmap = null;
            }
        }

        public object? OverContext => IsOverLink
            ? (ContextId >= 0 && ContextArray != null && ContextId < ContextArray.Count ? ContextArray[ContextId] : null)
            : null;
        public bool IsOverLink => wasOverLink;
        public int ContextId => overContextId;

        private int overContextId = -1;
        private bool wasOverLink = false;
        public override void Render(DrawingContext context)
        {
            base.Render(context);
            if (Background != null)
            {
                context.FillRectangle(Background, new Rect(Bounds.Size));
            }

            int newOverPartIndex = -1;
            double x = Padding.Left;
            double y = Padding.Top;
            bool wasWrapped = true;
            bool overLink = false;
            overContextId = -1;

            ParseText(this, Text, (text, partIndex, source, parameter, contextId, bitmap) =>
            {
                if (bitmap != null)
                {
                    context.DrawImage(bitmap, new Rect(x, y, 16, 16));
                    x += 16;
                }
                
                var styleId = parameter ? STYLE_PARAMETER : (source ? STYLE_SOURCE : (IsSelected ? STYLE_DEFAULT_SELECTED : STYLE_DEFAULT));
   
                Rect bounds;
                (wasWrapped, bounds) = drawer!.Draw(context, text, styleId, !wasWrapped, ref x, ref y, Padding.Left, Bounds.Width - Padding.Right);
                
                if (overPartIndex == partIndex && (source || parameter) && IsPointerOver)
                {
                    overLink = true;
                    overContextId = contextId;
                    drawer.DrawUnderline(context, styleId, bounds);
                }

                if (bounds.Contains(currentPos) && contextId != -1)
                    newOverPartIndex = partIndex;
            });

            overPartIndex = newOverPartIndex;
            
            if (overLink && !wasOverLink)
                PseudoClasses.Add(":overlink");
            else if (!overLink && wasOverLink)
                PseudoClasses.Remove(":overlink");

            wasOverLink = overLink;
        }

        public static Size MeasureText(float maxWidth, string text, Point testPoint, out int? overPartIndex)
        {
            EnsureDrawerInitialized();
            Rect bounds;
            bool wasWrapped = true;
            bool everWrapped = false;
            double x = 0, y = 0;
            int? hoverPartIndex = null;
            ParseText(null, text, (t, partIndex, source, parameter, contextId, bitmap) =>
            {
                if (bitmap != null)
                    x += 16;
                var styleId = parameter ? STYLE_PARAMETER : (source ? STYLE_SOURCE : STYLE_DEFAULT);
                (wasWrapped, bounds) = drawer.Measure(t, styleId, !wasWrapped, ref x, ref y, maxWidth);
                if (bounds.Contains(testPoint) && contextId != -1)
                    hoverPartIndex = partIndex;
                everWrapped |= wasWrapped;
            });
            overPartIndex = hoverPartIndex;
            var size = new Size(everWrapped ? maxWidth : x, y + drawer.LineHeight);
            return size;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var size = MeasureText((float)(availableSize.Width - Padding.Left - Padding.Right), Text, currentPos, out var hoverPartIndex);
            if (hoverPartIndex.HasValue)
                overPartIndex = hoverPartIndex.Value;
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
        
        public static readonly DirectProperty<FormattedTextBlock, bool> IsSelectedProperty =
            AvaloniaProperty.RegisterDirect<FormattedTextBlock, bool>(
                nameof(IsSelected),
                o => o.IsSelected,
                (o, v) => o.IsSelected = v);

        private bool isSelected; 
        public bool IsSelected
        {
            get => isSelected;
            set => SetAndRaise(IsSelectedProperty, ref isSelected, value);
        }
        
        public static readonly StyledProperty<IBrush?> BackgroundProperty =
            Border.BackgroundProperty.AddOwner<FormattedTextBlock>();

        /// <summary>
        /// Gets or sets a brush used to paint the control's background.
        /// </summary>
        public IBrush? Background
        {
            get => GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }
        
        /// <summary>
        /// Defines the <see cref="Padding"/> property.
        /// </summary>
        public static readonly StyledProperty<Thickness> PaddingProperty =
            Decorator.PaddingProperty.AddOwner<FormattedTextBlock>();

        private IList<object>? _ContextArray;
        public static readonly DirectProperty<FormattedTextBlock, IList<object>?> ContextArrayProperty = AvaloniaProperty.RegisterDirect<FormattedTextBlock, IList<object>?>("ContextArray", o => o.ContextArray, (o, v) => o.ContextArray = v);

        /// <summary>
        /// Gets or sets the padding to place around the <see cref="Text"/>.
        /// </summary>
        public Thickness Padding
        {
            get { return GetValue(PaddingProperty); }
            set { SetValue(PaddingProperty, value); }
        }

        public IList<object>? ContextArray
        {
            get { return _ContextArray; }
            set { SetAndRaise(ContextArrayProperty, ref _ContextArray, value); }
        }
    }
}
