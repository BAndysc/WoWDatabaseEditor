namespace WDE.Common.Database
{
    public interface IBroadcastText
    {
        uint Id { get; }
        uint Language { get; }
        string? Text { get; }
        string? Text1 { get; }
    }
}