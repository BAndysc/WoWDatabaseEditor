using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using WDE.Common.Database;
using WDE.Common.Types;

namespace WDE.Common.Avalonia.Controls;

public class GameClassesImage : BaseGameEnumImage
{
    public static readonly StyledProperty<CharacterClasses> GameClassesProperty = AvaloniaProperty.Register<GameClassesImage, CharacterClasses>(nameof(GameClasses));

    private static Task? cacheInProgress;
    
    private static List<int> enumValues = new()
    {
        (int)CharacterClasses.Warrior,
        (int)CharacterClasses.Paladin,
        (int)CharacterClasses.Hunter,
        (int)CharacterClasses.Rogue,
        (int)CharacterClasses.Priest,
        (int)CharacterClasses.DeathKnight,
        (int)CharacterClasses.Shaman,
        (int)CharacterClasses.Mage,
        (int)CharacterClasses.Warlock,
        (int)CharacterClasses.Monk,
        (int)CharacterClasses.Druid,
        (int)CharacterClasses.DemonHunter,
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

    protected override int Value => (int)GameClasses;
    protected override List<int> EnumValues => enumValues;
    protected override List<ImageUri> Images => images;
    protected override List<Bitmap?> CachedBitmaps => cachedBitmaps;
    protected override Task? CacheInProgress
    {
        get => cacheInProgress;
        set => cacheInProgress = value;
    }
}