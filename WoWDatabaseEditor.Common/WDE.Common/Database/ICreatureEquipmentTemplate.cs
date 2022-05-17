namespace WDE.Common.Database;

public interface ICreatureEquipmentTemplate
{
    uint Entry { get; }
    byte Id { get; }
    uint Item1 { get; }
    uint Item2 { get; }
    uint Item3 { get; }
    uint GetItem(int id);
}