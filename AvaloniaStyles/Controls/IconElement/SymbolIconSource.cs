using Avalonia;
using Avalonia.Controls;

namespace AvaloniaStyles.Controls;

/// <summary>
/// Represents an icon source that uses a glyph from the SymbolThemeFontFamily resource as its content.
/// </summary>
public class SymbolIconSource : IconSource
{
    /// <summary>
    /// Defines the <see cref="Symbol"/> property
    /// </summary>
    public static readonly StyledProperty<Symbol> SymbolProperty =
        SymbolIcon.SymbolProperty.AddOwner<SymbolIconSource>();

    /// <summary>
    /// Defines the <see cref="FontSize"/> property
    /// </summary>
    public static readonly StyledProperty<double> FontSizeProperty =
       TextBlock.FontSizeProperty.AddOwner<SymbolIconSource>();

    /// <summary>
    /// Gets or sets the <see cref="AvaloniaStyles.Controls.Symbol"/> this icon displays
    /// </summary>
    public Symbol Symbol
    {
        get => GetValue(SymbolProperty);
        set => SetValue(SymbolProperty, value);
    }

    /// <summary>
    /// Gets or sets the font size this icon uses when rendering
    /// </summary>
    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }
}
