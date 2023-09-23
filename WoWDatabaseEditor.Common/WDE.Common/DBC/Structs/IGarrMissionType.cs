namespace WDE.Common.DBC.Structs;

public interface IGarrMissionType
{
    public byte Id { get; }
    public string Name { get; }
    public int UiTextureAtlasMemberId { get; }
    public int UiTextureKitId { get; }
}

public class GarrMissionType : IGarrMissionType
{
    public byte Id { get; init; }
    public string Name { get; init; } = "";
    public int UiTextureAtlasMemberId { get; init; }
    public int UiTextureKitId { get; init; }
}