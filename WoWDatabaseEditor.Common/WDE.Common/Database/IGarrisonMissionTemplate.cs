namespace WDE.Common.Database;

public interface IGarrisonMissionTemplate
{
    int Entry { get; }
    uint LootId { get; }
    string Comment { get; }
}
