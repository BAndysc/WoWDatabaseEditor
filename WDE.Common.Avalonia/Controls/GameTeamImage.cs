using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using WDE.Common.Avalonia.Components;
using WDE.Common.Avalonia.Utils;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Types;

namespace WDE.Common.Avalonia.Controls;

public class GameTeamImage : Control
{
    public static readonly StyledProperty<CharacterRaces> RacesProperty = AvaloniaProperty.Register<GameTeamImage, CharacterRaces>(nameof(Races));
    public static readonly StyledProperty<GameTeamImageMode> ModeProperty = AvaloniaProperty.Register<GameTeamImage, GameTeamImageMode>(nameof(Mode));
    public static readonly StyledProperty<double> SpacingProperty = AvaloniaProperty.Register<GameTeamImage, double>(nameof(Spacing));
    public static readonly StyledProperty<bool> IgnoreIfBothProperty = AvaloniaProperty.Register<GameTeamImage, bool>(nameof(IgnoreIfBoth));

    public CharacterRaces Races
    {
        get => GetValue(RacesProperty);
        set => SetValue(RacesProperty, value);
    }
    
    public GameTeamImageMode Mode
    {
        get => GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    public bool IgnoreIfBoth
    {
        get => GetValue(IgnoreIfBothProperty);
        set => SetValue(IgnoreIfBothProperty, value);
    }

    private static Bitmap? allyImage, hordeImage;
    private static CharacterRaces cachedAllyRaces, cachedHordeRaces, cachedAllSupportedRaces;

    private static List<Bitmap?> cachedBitmaps = new();

    static GameTeamImage()
    {
        AffectsRender<GameTeamImage>(RacesProperty, ModeProperty, SpacingProperty);
        AffectsMeasure<GameTeamImage>(RacesProperty, ModeProperty, SpacingProperty);
        ClipToBoundsProperty.OverrideDefaultValue<GameTeamImage>(true);
    }

    private void CheckTeams(out bool isHorde, out bool isAlly)
    {
        if (allyImage == null)
        {
            var coreVersion = ViewBind.ResolveViewModel<ICurrentCoreVersion>().Current;
            cachedAllSupportedRaces = coreVersion.GameVersionFeatures.AllRaces;
            cachedAllyRaces = cachedAllSupportedRaces & CharacterRaces.AllAlliance;
            cachedHordeRaces = cachedAllSupportedRaces & CharacterRaces.AllHorde;
            allyImage = WdeImage.LoadBitmap(new ImageUri("Icons/icon_alliance.png"));
            hordeImage = WdeImage.LoadBitmap(new ImageUri("Icons/icon_horde.png"));
        }

        var races = Races;
        var mode = Mode;
        if (mode == GameTeamImageMode.MustContainAll)
        {
            isAlly = races == cachedAllyRaces;
            isHorde = races == cachedHordeRaces;
        }
        else if (mode == GameTeamImageMode.MustContainAny)
        {
            isAlly = (races & cachedAllyRaces) != 0;
            isHorde = (races & cachedHordeRaces) != 0;
        }
        else
            throw new Exception("Unknown mode");

        if (IgnoreIfBoth)
        {
            if (isHorde && isAlly)
            {
                isHorde = false;
                isAlly = false;
            }
        }
    }
    
    protected override Size MeasureOverride(Size availableSize)
    {
        CheckTeams(out var isHorde, out var isAlly);
        var count = (isHorde ? 1 : 0) + (isAlly ? 1 : 0);
        var spacing = Spacing;
        var size = Math.Min(availableSize.Width, availableSize.Height);
        if (double.IsInfinity(size))
            size = 1024;

        return new Size(count * size + Math.Max(0, count - 1) * spacing, size);
    }

    public override void Render(DrawingContext context)
    {
        CheckTeams(out var isHorde, out var isAlly);
        var size = Math.Min(Bounds.Width, Bounds.Height);
        double x = 0;
        if (isAlly)
        {
            var rect = new Rect(x, 0, size, size);
            x += size + Spacing;
            context.DrawImage(allyImage, rect);
        }

        if (isHorde)
        {
            var rect = new Rect(x, 0, size, size);
            x += size + Spacing;
            context.DrawImage(hordeImage, rect);
        }
    }
}

public enum GameTeamImageMode
{
    MustContainAll,
    MustContainAny,
}