namespace WDE.QueryGenerators.Models;

public readonly struct ConditionDeleteModel
{
    public readonly int SourceTypeOrReferenceId;
    public readonly List<long>? BySourceGroup;
    public readonly List<long>? BySourceEntry;
    public readonly List<long>? BySourceId;

    private ConditionDeleteModel(int type, List<long>? group, List<long>? entry, List<long>? id)
    {
        SourceTypeOrReferenceId = type;
        BySourceGroup = group;
        BySourceEntry = entry;
        BySourceId = id;
    }

    public static ConditionDeleteModel ByGroup(int type, List<long> group)
    {
        return new ConditionDeleteModel(type, group, null, null);
    }
    
    public static ConditionDeleteModel ByEntry(int type, List<long> entry)
    {
        return new ConditionDeleteModel(type, null, entry, null);
    }
    
    public static ConditionDeleteModel ById(int type, List<long> id)
    {
        return new ConditionDeleteModel(type, null, null, id);
    }
}