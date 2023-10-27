namespace WDE.Common.DBC.Structs;

public interface ICurrencyType
{
    uint Id { get; }
    string Name { get; }
    string Description { get; }
    uint MaxQuantity { get; }
    uint MaxEarnablePerWeek { get; }
    CurrencyTypesFlags Flags { get; }
    byte CategoryId { get; }
    byte SpellCategory { get; }
    byte Quality { get; }
    string? InventoryIconPath { get; }
    uint? InventoryIconFileId { get; }
    uint SpellWeight { get; }
    ICurrencyCategory? Category { get; }
    
    bool UsesFileId => InventoryIconFileId.HasValue;
    bool UsesFilePath => InventoryIconPath != null;
}

public class CurrencyType : ICurrencyType
{
    public uint Id { get; init; }
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public uint MaxQuantity { get; init; }
    public uint MaxEarnablePerWeek { get; init; }
    public CurrencyTypesFlags Flags { get; init; }
    public byte CategoryId { get; init; }
    public byte SpellCategory { get; init; }
    public byte Quality { get; init; }
    public string? InventoryIconPath { get; init; }
    public uint? InventoryIconFileId { get; init; }
    public uint SpellWeight { get; init; }
    public ICurrencyCategory? Category { get; set; }
}

public enum CurrencyTypesFlags
{
    Tradable                            = 0x00000001,
    AppearsInLootWindow                 = 0x00000002,
    ComputedWeeklyMaximum               = 0x00000004,
    _100_Scaler                         = 0x00000008,
    NoLowLevelDrop                      = 0x00000010,
    IgnoreMaxQtyOnLoad                  = 0x00000020,
    LogOnWorldChange                    = 0x00000040,
    TrackQuantity                       = 0x00000080,
    ResetTrackedQuantity                = 0x00000100,
    UpdateVersionIgnoreMax              = 0x00000200,
    SuppressChatMessageOnVersionChange  = 0x00000400,
    SingleDropInLoot                    = 0x00000800,
    HasWeeklyCatchup                    = 0x00001000,
    DoNotCompressChat                   = 0x00002000,
    DoNotLogAcquisitionToBi             = 0x00004000,
    NoRaidDrop                          = 0x00008000,
    NotPersistent                       = 0x00010000,
    Deprecated                          = 0x00020000,
    DynamicMaximum                      = 0x00040000,
    SuppressChatMessages                = 0x00080000,
    DoNotToast                          = 0x00100000,
    DestroyExtraOnLoot                  = 0x00200000,
    DontShowTotalInTooltip              = 0x00400000,
    DontCoalesceInLootWindow            = 0x00800000,
    AccountWide                         = 0x01000000,
    AllowOverflowMailer                 = 0x02000000,
    HideAsReward                        = 0x04000000,
    HasWarmodeBonus                     = 0x08000000,
    IsAllianceOnly                      = 0x10000000,
    IsHordeOnly                         = 0x20000000,
    LimitWarmodeBonusOncePerTooltip     = 0x40000000,
}