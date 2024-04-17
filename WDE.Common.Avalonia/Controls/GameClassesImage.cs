using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using WDE.Common.Avalonia.Utils;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Types;

namespace WDE.Common.Avalonia.Controls;

public class GameClassesImage : BaseGameEnumImage
{
    public static readonly StyledProperty<CharacterClasses> GameClassesProperty = AvaloniaProperty.Register<GameClassesImage, CharacterClasses>(nameof(GameClasses));

    private static Task? cacheInProgress;

    private static CharacterClasses? cachedAllClasses;

    public static IPen RedPen = new Pen(Brushes.Red, 2);

    private static List<uint> enumValues = new()
    {
        (uint)CharacterClasses.Warrior,
        (uint)CharacterClasses.Paladin,
        (uint)CharacterClasses.Hunter,
        (uint)CharacterClasses.Rogue,
        (uint)CharacterClasses.Priest,
        (uint)CharacterClasses.DeathKnight,
        (uint)CharacterClasses.Shaman,
        (uint)CharacterClasses.Mage,
        (uint)CharacterClasses.Warlock,
        (uint)CharacterClasses.Monk,
        (uint)CharacterClasses.Druid,
        (uint)CharacterClasses.DemonHunter,
    };
    
    private static List<CharacterClasses> enums = new()
    {
        CharacterClasses.Warrior,
        CharacterClasses.Paladin,
        CharacterClasses.Hunter,
        CharacterClasses.Rogue,
        CharacterClasses.Priest,
        CharacterClasses.DeathKnight,
        CharacterClasses.Shaman,
        CharacterClasses.Mage,
        CharacterClasses.Warlock,
        CharacterClasses.Monk,
        CharacterClasses.Druid,
        CharacterClasses.DemonHunter,
    };
    
    private static List<ImageUri> images = new()
    {
        new ImageUri("Icons/classes/warrior_big.png"),
        new ImageUri("Icons/classes/paladin_big.png"),
        new ImageUri("Icons/classes/hunter_big.png"),
        new ImageUri("Icons/classes/rogue_big.png"),
        new ImageUri("Icons/classes/priest_big.png"),
        new ImageUri("Icons/classes/death_knight_big.png"),
        new ImageUri("Icons/classes/shaman_big.png"),
        new ImageUri("Icons/classes/mage_big.png"),
        new ImageUri("Icons/classes/warlock_big.png"),
        new ImageUri("Icons/classes/monk_big.png"),
        new ImageUri("Icons/classes/druid_big.png"),
        new ImageUri("Icons/classes/demon_hunter_big.png")
    };

    private static Dictionary<CharacterClasses, int> classToIndex = new()
    {
        [CharacterClasses.Warrior] = 0,
        [CharacterClasses.Paladin] = 1,
        [CharacterClasses.Hunter] = 2,
        [CharacterClasses.Rogue] = 3,
        [CharacterClasses.Priest] = 4,
        [CharacterClasses.DeathKnight] = 5,
        [CharacterClasses.Shaman] = 6,
        [CharacterClasses.Mage] = 7,
        [CharacterClasses.Warlock] = 8,
        [CharacterClasses.Monk] = 9,
        [CharacterClasses.Druid] = 10,
        [CharacterClasses.DemonHunter] = 11
    };

    private static List<Bitmap?> cachedBitmaps = new();

    public CharacterClasses GameClasses
    {
        get => (CharacterClasses)GetValue(GameClassesProperty);
        set => SetValue(GameClassesProperty, value);
    }

    static GameClassesImage()
    {
        AffectsRender<GameClassesImage>(GameClassesProperty);
        AffectsMeasure<GameClassesImage>(GameClassesProperty);
        ClipToBoundsProperty.OverrideDefaultValue<GameClassesImage>(true);
        GameClassesProperty.Changed.AddClassHandler<GameClassesImage>((x, e) =>
        {
            var enumValue = x.GameClasses;
            string? tooltip = null;
            if (enumValue == 0)
            {
                tooltip = null;
            }
            else
            {
                tooltip = string.Join(", ", enums.Where(e => enumValue.HasFlagFast(e))
                    .Select(Enum.GetName));
            }
            ToolTip.SetTip(x, tooltip);
        });
    }

    private void CheckClasses(out bool allClasses,
        out CharacterClasses allButClass1,
        out CharacterClasses allButClass2)
    {
        allButClass1 = 0;
        allButClass2 = 0;

        if (!cachedAllClasses.HasValue)
        {
            var coreVersion = ViewBind.ResolveViewModel<ICurrentCoreVersion>().Current;
            cachedAllClasses = coreVersion.GameVersionFeatures.AllClasses;
        }

        var classes = GameClasses;
        allClasses = classes == cachedAllClasses.Value;

        int missing = 0;

        foreach (var e in enums)
        {
            if (!classes.HasFlagFast(e))
            {
                switch (missing++)
                {
                    case 0:
                        allButClass1 = e;
                        break;
                    case 1:
                        allButClass2 = e;
                        break;
                    default:
                        break;
                }
            }
        }

        if (missing > 2)
            allButClass1 = allButClass2 = 0;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        CheckClasses(out var allClasses, out var allButClass1, out var allButClass2);
        if (allClasses)
            return default;

        if (allButClass1 != 0)
        {
            var numToDraw = (allButClass1 != 0 ? 1 : 0) + (allButClass2 != 0 ? 1 : 0);

            var size = Math.Min(availableSize.Width, availableSize.Height);
            if (double.IsInfinity(size))
                size = 1024;
            var maxSizeDueToWidth = double.IsInfinity(availableSize.Width) || double.IsNaN(availableSize.Width) ? 1024 : availableSize.Width / numToDraw;
            size = Math.Min(size, maxSizeDueToWidth);

            return new Size(numToDraw * size + Math.Max(0, numToDraw - 1) * Spacing, size);
        }

        return base.MeasureOverride(availableSize);
    }

    public override void Render(DrawingContext context)
    {
        CheckClasses(out var allClasses, out var allButClass1, out var allButClass2);
        if (allClasses)
            return;

        if (allButClass1 != 0)
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

            if (!CacheBitmaps(CachedBitmaps, Images))
                return;

            if (classToIndex.TryGetValue(allButClass1, out var img))
                DrawImage(cachedBitmaps[img], true);
            if (allButClass2 != 0 && classToIndex.TryGetValue(allButClass2, out img))
                DrawImage(cachedBitmaps[img], true);
        }
        else
            base.Render(context);
    }

    protected override uint Value => (uint)GameClasses;
    protected override List<uint> EnumValues => enumValues;
    protected override List<ImageUri> Images => images;
    protected override List<Bitmap?> CachedBitmaps => cachedBitmaps;
    protected override Task? CacheInProgress
    {
        get => cacheInProgress;
        set => cacheInProgress = value;
    }
}