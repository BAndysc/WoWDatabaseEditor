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
    
    public static IPen RedPen = new Pen(Brushes.Red, 2);
    
    private static List<uint> enumValues = new()
    {
        (uint)CharacterRaces.Human,
        (uint)CharacterRaces.Orc,
        (uint)CharacterRaces.Dwarf,
        (uint)CharacterRaces.NightElf,
        (uint)CharacterRaces.Undead,
        (uint)CharacterRaces.Tauren,
        (uint)CharacterRaces.Gnome,
        (uint)CharacterRaces.Troll,
        (uint)CharacterRaces.Goblin,
        (uint)CharacterRaces.BloodElf,
        (uint)CharacterRaces.Draenei,
        (uint)CharacterRaces.Worgen,
        (uint)CharacterRaces.Pandaren,
        (uint)CharacterRaces.PandarenAlliance,
        (uint)CharacterRaces.PandarenHorde,
        (uint)CharacterRaces.VoidElf,
        (uint)CharacterRaces.HighmountainTauren,
        (uint)CharacterRaces.LightforgedDraenei,
        (uint)CharacterRaces.Nightborne,
        (uint)CharacterRaces.ZandalariTroll,
        (uint)CharacterRaces.KulTiran,
        (uint)CharacterRaces.DarkIronDwarf,
        (uint)CharacterRaces.Vulpera,
        (uint)CharacterRaces.MagharOrc,
        (uint)CharacterRaces.Mechagnome,
        (uint)CharacterRaces.DracthyrAlliance,
        (uint)CharacterRaces.DracthyrHorde
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
        CharacterRaces.PandarenHorde,
        CharacterRaces.VoidElf,
        CharacterRaces.HighmountainTauren,
        CharacterRaces.LightforgedDraenei,
        CharacterRaces.Nightborne,
        CharacterRaces.ZandalariTroll,
        CharacterRaces.KulTiran,
        CharacterRaces.DarkIronDwarf,
        CharacterRaces.Vulpera,
        CharacterRaces.MagharOrc,
        CharacterRaces.Mechagnome,
        CharacterRaces.DracthyrAlliance,
        CharacterRaces.DracthyrHorde
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
        new ImageUri("Icons/races/voidelf.png"),
        new ImageUri("Icons/races/highmountaintauren.png"),
        new ImageUri("Icons/races/lightforgeddraenei.png"),
        new ImageUri("Icons/races/nightborne.png"),
        new ImageUri("Icons/icon_unknown.png"),
        new ImageUri("Icons/icon_unknown.png"),
        new ImageUri("Icons/icon_unknown.png"),
        new ImageUri("Icons/icon_unknown.png"),
        new ImageUri("Icons/icon_unknown.png"),
        new ImageUri("Icons/icon_unknown.png"),
        new ImageUri("Icons/icon_unknown.png"),
        new ImageUri("Icons/icon_unknown.png"),
    };

    private static Dictionary<CharacterRaces, int> raceToIndex = new()
    {
        [CharacterRaces.Human] = 0,
        [CharacterRaces.Orc] = 1,
        [CharacterRaces.Dwarf] = 2,
        [CharacterRaces.NightElf] = 3,
        [CharacterRaces.Undead] = 4,
        [CharacterRaces.Tauren] = 5,
        [CharacterRaces.Gnome] = 6,
        [CharacterRaces.Troll] = 7,
        [CharacterRaces.Goblin] = 8,
        [CharacterRaces.BloodElf] = 9,
        [CharacterRaces.Draenei] = 10,
        [CharacterRaces.Worgen] = 11,
        [CharacterRaces.Pandaren] = 12,
        [CharacterRaces.PandarenAlliance] = 13,
        [CharacterRaces.PandarenHorde] = 14,
        [CharacterRaces.VoidElf] = 15,
        [CharacterRaces.HighmountainTauren] = 16,
        [CharacterRaces.LightforgedDraenei] = 17,
        [CharacterRaces.Nightborne] = 18,
        [CharacterRaces.ZandalariTroll] = 19,
        [CharacterRaces.KulTiran] = 20,
        [CharacterRaces.DarkIronDwarf] = 21,
        [CharacterRaces.Vulpera] = 22,
        [CharacterRaces.MagharOrc] = 23,
        [CharacterRaces.Mechagnome] = 24,
        [CharacterRaces.DracthyrAlliance] = 25,
        [CharacterRaces.DracthyrHorde] = 26
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

    private void CheckTeams(out bool allRaces, out bool isHordeOnly, out bool isAllyOnly,
        out CharacterRaces hordeButRace1,
        out CharacterRaces hordeButRace2,
        out CharacterRaces allyButRace1,
        out CharacterRaces allyButRace2)
    {
        hordeButRace1 = 0;
        hordeButRace2 = 0;
        allyButRace1 = 0;
        allyButRace2 = 0;
        
        if (allyImage == null)
        {
            var coreVersion = ViewBind.ResolveViewModel<ICurrentCoreVersion>().Current;
            cachedAllSupportedRaces = coreVersion.GameVersionFeatures.AllRaces;
            cachedAllyRaces = cachedAllSupportedRaces & CharacterRaces.AllAlliance;
            cachedHordeRaces = cachedAllSupportedRaces & CharacterRaces.AllHorde;
        }

        var races = Races;
        isAllyOnly = (races & cachedAllyRaces) == cachedAllyRaces;
        isHordeOnly = (races & cachedHordeRaces) == cachedHordeRaces;
        
        int missingHorde = 0;
        int missingAlly = 0;
        
        foreach (var e in enums)
        {
            if ((e & cachedAllSupportedRaces) == 0) // for "all but" we only check supported races
                continue;

            var isAlly = (e & CharacterRaces.AllAlliance) != 0;
            var isHorde = (e & CharacterRaces.AllHorde) != 0;

            if (!races.HasFlagFast(e))
            {
                if (isHorde)
                {
                    switch (missingHorde++)
                    {
                        case 0:
                            hordeButRace1 = e;
                            break;
                        case 1:
                            hordeButRace2 = e;
                            break;
                        default:
                            break;
                    }
                }
                else if (isAlly)
                {
                    switch (missingAlly++)
                    {
                        case 0:
                            allyButRace1 = e;
                            break;
                        case 1:
                            allyButRace2 = e;
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        
        if (missingHorde > 2)
            hordeButRace1 = hordeButRace2 = 0;
        
        if (missingAlly > 2)
            allyButRace1 = allyButRace2 = 0;
        
        allRaces = (races & cachedAllSupportedRaces) == cachedAllSupportedRaces;
        if (IgnoreIfAllPerTeam)
        {
            // if any of 'all ally' or 'all horde' is true, then set to 'all races' as we want to ignore the whole team
            allRaces |= isAllyOnly || isHordeOnly;
        }
    }
    
    protected override Size MeasureOverride(Size availableSize)
    {
        CheckTeams(out var allSupported, out var onlyHorde, out var onlyAlly,
            out CharacterRaces hordeButRace1,
            out CharacterRaces hordeButRace2,
            out CharacterRaces allyButRace1,
            out CharacterRaces allyButRace2);
        
        if (allSupported)
            return default;
        
        if (onlyAlly || onlyHorde)
        {
            return MeasureInternal(1, Value &~ (uint)(onlyAlly ? cachedAllyRaces : cachedHordeRaces), availableSize);
        }

        if (hordeButRace1 != 0 || allyButRace1 != 0)
        {
            int count = (hordeButRace1 != 0 ? 2 : 0) + (hordeButRace2 != 0 ? 1 : 0) + (allyButRace1 != 0 ? 2 : 0) + (allyButRace2 != 0 ? 1 : 0);
            return MeasureInternal(count, 0, availableSize);
        }

        return base.MeasureOverride(availableSize);
    }

    public override void Render(DrawingContext context)
    {
        double x = 0;
        var size = Math.Min(Bounds.Width, Bounds.Height);

        void DrawImage(Bitmap? bitmap, bool excluded)
        {
            if (bitmap != null)
                context.DrawImage(bitmap, new Rect(x, 0, size, size));
            if (excluded)
                context.DrawLine(RedPen, new Point(x, 0), new Point(x + size, size));
            x += size + Spacing;
        }
        
        CheckTeams(out var allSupported, out var onlyHorde, out var onlyAlly,
            out CharacterRaces hordeButRace1,
            out CharacterRaces hordeButRace2,
            out CharacterRaces allyButRace1,
            out CharacterRaces allyButRace2);

        if (allSupported)
            return;

        if (onlyAlly || onlyHorde)
        {
            var image = onlyAlly ? allyImage : hordeImage;
            DrawImage(image, false);
            DrawAt(context, Value &~ (uint)(onlyAlly ? cachedAllyRaces : cachedHordeRaces), x + Spacing);
        }
        else if (hordeButRace1 != 0 || allyButRace1 != 0)
        {
            if (!CacheBitmaps(CachedBitmaps, Images))
                return;
            
            if (hordeButRace1 != 0)
            {
                DrawImage(hordeImage, false);
                if (raceToIndex.TryGetValue(hordeButRace1, out var img))
                    DrawImage(cachedBitmaps[img], true);
                if (hordeButRace2 != 0 && raceToIndex.TryGetValue(hordeButRace2, out img))
                    DrawImage(cachedBitmaps[img], true);
            }
            
            if (allyButRace1 != 0)
            {
                DrawImage(allyImage, false);
                if (raceToIndex.TryGetValue(allyButRace1, out var img))
                    DrawImage(cachedBitmaps[img], true);
                if (allyButRace2 != 0 && raceToIndex.TryGetValue(allyButRace2, out img))
                    DrawImage(cachedBitmaps[img], true);
            }
        }
        else
            base.Render(context);
    }

    protected override uint Value => (uint)Races;
    protected override List<uint> EnumValues => enumValues;
    protected override List<ImageUri> Images => images;
    protected override List<Bitmap?> CachedBitmaps => cachedBitmaps;
    protected override Task? CacheInProgress
    {
        get => cacheInProgress;
        set => cacheInProgress = value;
    }
}