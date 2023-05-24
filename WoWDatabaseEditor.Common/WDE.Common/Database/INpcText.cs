namespace WDE.Common.Database
{
    public interface INpcText
    {
        uint Id { get; }
        uint BroadcastTextId { get; }
        string? Text0_0 { get; }
        string? Text0_1 { get; }
    }
}