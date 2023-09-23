namespace WDE.Common.DBC.Structs;

public interface IUiTextureKit
{
    int Id { get; }
    string KitPrefix { get; }
}

public class UiTextureKit : IUiTextureKit
{
    public int Id { get; init; }
    public string KitPrefix { get; init; } = "";
}