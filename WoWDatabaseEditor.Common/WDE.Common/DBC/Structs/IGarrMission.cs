namespace WDE.Common.DBC.Structs;

public interface IGarrMission
{
    string Name { get; }    
    string Description { get; }
    string Location { get; }
    int MissionDuration { get; }
    uint OfferDuration { get; }
    float MapPos0 { get; }
    float MapPos1 { get; }
    float WorldPos0 { get; }
    float WorldPos1 { get; }
    ushort TargetItemLevel { get; }
    ushort UiTextureKitId { get; }
    IUiTextureKit? UiTextureKit { get; }
    ushort MissionCostCurrencyTypeId { get; }
    ICurrencyType? MissionCostCurrencyType { get; }
    byte TargetLevel { get; }
    sbyte EnvGarrMechanicTypeId { get; }
    byte MaxFollowers { get; }
    byte OfferedGarrMissionTextureId { get; }
    byte GarrMissionTypeId { get; }
    IGarrMissionType? GarrMissionType { get; }
    byte GarrFollowerTypeId { get; }
    byte BaseCompletionChance { get; }
    byte FollowerDeathChance { get; }
    byte GarrTypeId { get; }
    int Id { get; }
    int TravelDuration { get; }
    uint PlayerConditionId { get; }
    uint MissionCost { get; }
    uint Flags { get; }
    uint BaseFollowerXp { get; }
    uint AreaId { get; }
    uint OvermaxRewardPackId { get; }
    uint EnvGarrMechanicId { get; }
    uint GarrMissionSetId { get; }
}

public class GarrMission : IGarrMission
{
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public string Location { get; init; } = "";
    public int MissionDuration { get; init; }
    public uint OfferDuration { get; init; }
    public float MapPos0 { get; init; }
    public float MapPos1 { get; init; }
    public float WorldPos0 { get; init; }
    public float WorldPos1 { get; init; }
    public ushort TargetItemLevel { get; init; }
    public ushort UiTextureKitId { get; init; }
    public IUiTextureKit? UiTextureKit { get; set; }
    public ushort MissionCostCurrencyTypeId { get; init; }
    public ICurrencyType? MissionCostCurrencyType { get; set; }
    public byte TargetLevel { get; init; }
    public sbyte EnvGarrMechanicTypeId { get; init; }
    public byte MaxFollowers { get; init; }
    public byte OfferedGarrMissionTextureId { get; init; }
    public byte GarrMissionTypeId { get; init; }
    public IGarrMissionType? GarrMissionType { get; set; }
    public byte GarrFollowerTypeId { get; init; }
    public byte BaseCompletionChance { get; init; }
    public byte FollowerDeathChance { get; init; }
    public byte GarrTypeId { get; init; }
    public int Id { get; init; }
    public int TravelDuration { get; init; }
    public uint PlayerConditionId { get; init; }
    public uint MissionCost { get; init; }
    public uint Flags { get; init; }
    public uint BaseFollowerXp { get; init; }
    public uint AreaId { get; init; }
    public uint OvermaxRewardPackId { get; init; }
    public uint EnvGarrMechanicId { get; init; }
    public uint GarrMissionSetId { get; init; }
}