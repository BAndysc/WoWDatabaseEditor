namespace WDE.Common.Database;

public interface IBaseEquipmentTemplate
{
    uint Item1 { get; }
    uint Item2 { get; }
    uint Item3 { get; }
    uint GetItem(int id);
}

public interface ICreatureEquipmentTemplate : IBaseEquipmentTemplate
{
    uint Entry { get; }
    byte Id { get; }
}

// in mangos, Entry is a global unique id, instead of creature Id
public interface IMangosCreatureEquipmentTemplate : IBaseEquipmentTemplate
{
    uint Entry { get; }
}