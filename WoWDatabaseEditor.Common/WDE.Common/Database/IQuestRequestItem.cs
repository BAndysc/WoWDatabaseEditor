namespace WDE.Common.Database;

public interface IQuestRequestItem
{
    public uint Entry { get; }
    public uint EmoteOnComplete { get; }
    public uint EmoteOnIncomplete { get; }
    public string? CompletionText { get; }
    public int VerifiedBuild { get; }
}