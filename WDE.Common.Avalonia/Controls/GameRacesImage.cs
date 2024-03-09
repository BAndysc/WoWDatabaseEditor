using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

public class GameRacesImage : BaseGameEnumImage
{
    public static readonly StyledProperty<CharacterRaces> RacesProperty = AvaloniaProperty.Register<GameRacesImage, CharacterRaces>(nameof(Races));
    public static readonly StyledProperty<bool> IgnoreIfAllPerTeamProperty = AvaloniaProperty.Register<GameRacesImage, bool>(nameof(IgnoreIfAllPerTeam));

    private static Task? cacheInProgress;
    
    public CharacterRaces Races
    {
        get => GetValue(RacesProperty);
        set => SetValue(RacesProperty, value);
    }
    
    public bool IgnoreIfAllPerTeam
    {
        get => GetValue(IgnoreIfAllPerTeamProperty);
        set => SetValue(IgnoreIfAllPerTeamProperty, value);
    }
    
    private static List<int> enumValues = new()
    {
        (int)CharacterRaces.Human,
        (int)CharacterRaces.Orc,
        (int)CharacterRaces.Dwarf,
        (int)CharacterRaces.NightElf,
        (int)CharacterRaces.Undead,
        (int)CharacterRaces.Tauren,
        (int)CharacterRaces.Gnome,
        (int)CharacterRaces.Troll,
        (int)CharacterRaces.Goblin,
        (int)CharacterRaces.BloodElf,
        (int)CharacterRaces.Draenei,
        (int)CharacterRaces.Worgen,
        (int)CharacterRaces.Pandaren,
        (int)CharacterRaces.PandarenAlliance,
        (int)CharacterRaces.PandarenHorde
    };
    
    private static List<CharacterRaces> enums = new()
    {
        CharacterRaces.Human,
        CharacterRaces.Orc,
        CharacterRaces.Dwarf,
        CharacterRaces.NightElf,
        CharacterRaces.Undead,
        CharacterRaces.Tauren,
        CharacterRaces.Gnome,
        CharacterRaces.Troll,
        CharacterRaces.Goblin,
        CharacterRaces.BloodElf,
        CharacterRaces.Draenei,
        CharacterRaces.Worgen,
        CharacterRaces.Pandaren,
        CharacterRaces.PandarenAlliance,
        CharacterRaces.PandarenHorde
    };
    
    private static List<ImageUri> images = new()
    {
        new ImageUri("Icons/races/human.png"),
        new ImageUri("Icons/races/orc.png"),
        new ImageUri("Icons/races/dwarf.png"),
        new ImageUri("Icons/races/night_elf.png"),
        new ImageUri("Icons/races/undead.png"),
        new ImageUri("Icons/races/tauren.png"),
        new ImageUri("Icons/races/gnome.png"),
        new ImageUri("Icons/races/troll.png"),
        new ImageUri("Icons/races/goblin.png"),
        new ImageUri("Icons/races/blood_elf.png"),
        new ImageUri("Icons/races/draenei.png"),
        new ImageUri("Icons/races/worgen.png"),
        new ImageUri("Icons/races/pandaren.png"),
        new ImageUri("Icons/races/pandaren.png"),
        new ImageUri("Icons/races/pandaren.png"),
    };

    private static Bitmap? allyImage, hordeImage;
    private static CharacterRaces cachedAllyRaces, cachedHordeRaces, cachedAllSupportedRaces;

    private static List<Bitmap?> cachedBitmaps = new();

    static GameRacesImage()
    {
        AffectsRender<GameRacesImage>(RacesProperty);
        AffectsMeasure<GameRacesImage>(RacesProperty);
        ClipToBoundsProperty.OverrideDefaultValue<GameRacesImage>(true);
        RacesProperty.Changed.AddClassHandler<GameRacesImage>((x, e) =>
        {
            var enumValue = x.Races;
            string? tooltip = null;
            if (enumValue == 0)
            {
                tooltip = null;
            }
            else
            {
                tooltip = string.Join(", ", enums.Where(e => (e & enumValue) != 0)
                    .Select(Enum.GetName));
            }
            ToolTip.SetTip(x, tooltip);
        });
    }

    protected override async Task AdditionalCacheTaskAsync()
    {
        allyImage = await WdeImage.LoadBitmapAsync(new ImageUri("Icons/icon_alliance.png"));
        hordeImage = await WdeImage.LoadBitmapAsync(new ImageUri("Icons/icon_horde.png"));
    }

    private void CheckTeams(out bool allRaces, out bool isHordeOnly, out bool isAllyOnly)
    {
        if (allyImage == null)
        {
            var coreVersion = ViewBind.ResolveViewModel<ICurrentCoreVersion>().Current;
            cachedAllSupportedRaces = coreVersion.GameVersionFeatures.AllRaces;
            cachedAllyRaces = cachedAllSupportedRaces & CharacterRaces.AllAlliance;
            cachedHordeRaces = cachedAllSupportedRaces & CharacterRaces.AllHorde;
        }

        var races = Races;
        isAllyOnly = races == cachedAllyRaces;
        isHordeOnly = races == cachedHordeRaces;
        allRaces = (races & cachedAllSupportedRaces) == cachedAllSupportedRaces;
        if (IgnoreIfAllPerTeam)
        {
            // if any of 'all ally' or 'all horde' is true, then set to 'all races' as we want to ignore the whole team
            allRaces |= isAllyOnly || isHordeOnly;
        }
    }
    
    protected override Size MeasureOverride(Size availableSize)
    {
        CheckTeams(out var allSupported, out var onlyHorde, out var onlyAlly);
        if (allSupported)
            return default;
        if (onlyAlly || onlyHorde)
        {
            var size = Math.Min(availableSize.Width, availableSize.Height);
            if (double.IsInfinity(size))
                size = 1024;
            return new Size(size, size);
        }
        return base.MeasureOverride(availableSize);
    }

    public override void Render(DrawingContext context)
    {
        CheckTeams(out var allSupported, out var onlyHorde, out var onlyAlly);
        if (allSupported)
            return;
        if (onlyAlly || onlyHorde)
        {
            var size = Math.Min(Bounds.Width, Bounds.Height);
            var rect = new Rect(0, 0, size, size);
            var image = onlyAlly ? allyImage : hordeImage;
            if (image != null)
                context.DrawImage(image, rect);        
        }
        else
            base.Render(context);
    }

    protected override int Value => (int)Races;
    protected override List<int> EnumValues => enumValues;
    protected override List<ImageUri> Images => images;
    protected override List<Bitmap?> CachedBitmaps => cachedBitmaps;
    protected override Task? CacheInProgress
    {
        get => cacheInProgress;
        set => cacheInProgress = value;
    }
}