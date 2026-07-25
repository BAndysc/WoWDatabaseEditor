namespace WDE.MpqReader.DBC;

public readonly struct ManagedString
{
    private static List<string> all = new()
    {
        "--Invalid--"
    };

    private readonly int Index;

    private ManagedString(int index)
    {
        Index = index;
    }

    public static ManagedString Empty => default;

    public static ManagedString Create(string str)
    {
        all.Add(str);
        return new ManagedString(all.Count - 1);
    }

    public override string ToString()
    {
        return all[Index];
    }

    public static implicit operator string(ManagedString str) =>
        str.ToString();

}