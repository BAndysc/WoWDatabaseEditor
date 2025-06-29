namespace WDE.Common.Database;

public interface IQuestFactionChange
{
    uint AllianceQuestId { get; }
    uint HordeQuestId { get; }
}