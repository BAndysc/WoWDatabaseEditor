#nullable disable
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AvaloniaStyles.Controls;

/// <summary>
/// Represents an icon that uses a glyph from the specified font.
/// </summary>
public partial class FontIcon : FAIconElement
{
    protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TextBlock.FontSizeProperty ||
            change.Property == TextBlock.FontFamilyProperty ||
            change.Property == TextBlock.FontWeightProperty ||
            change.Property == TextBlock.FontStyleProperty ||
            change.Property == GlyphProperty)
        {
            _textLayout = null;
            InvalidateMeasure();
        }
        else if (change.Property == TextBlock.ForegroundProperty)
        {
            _textLayout = null;
            // FAIconElement calls InvalidateVisual
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (_textLayout == null)
        {
            GenerateText();
        }

        return _textLayout.Size;
    }

    public override void Render(DrawingContext context)
    {
        if (_textLayout == null)
            GenerateText();

        var dstRect = new Rect(Bounds.Size);
        using (context.PushClip(dstRect))
        {
            var pt = new Point(dstRect.Center.X - _textLayout.Size.Width / 2,
                               dstRect.Center.Y - _textLayout.Size.Height / 2);
            using var _ = context.PushPreTransform(Matrix.CreateTranslation(pt));
            _textLayout.Draw(context); // , pt todo Avalonia 11
        }
    }

    private void GenerateText()
    {
        _textLayout = new TextLayout(Glyph, new Typeface(FontFamily, FontStyle, FontWeight),
           FontSize, Foreground, TextAlignment.Left);
    }

    private TextLayout _textLayout;
}
