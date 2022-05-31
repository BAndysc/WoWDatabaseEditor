using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class Map
{
    public readonly int Id;
    public readonly string Directory;
    public readonly string Name;

    public Map(IDbcIterator dbcIterator)
    {
        Id = dbcIterator.GetInt(0);
        Directory = dbcIterator.GetString(1);
        Name = dbcIterator.GetString(5);
    }

    private Map()
    {
        Id = -1;
        Directory = "";
        Name = "(null)";
    }

    public static Map Empty => new Map();
}