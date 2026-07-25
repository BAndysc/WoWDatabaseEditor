namespace WDE.MpqReader.DBC;

public readonly struct ManagedString
{
    // Create is called from both the game loop and thread-pool workers (model parsing),
    // so the pool must be guarded; interning also keeps the list finite (paths repeat a lot).
    private static readonly object sync = new();
    private static readonly List<string> all = new()
    {
        "--Invalid--"
    };
    private static readonly Dictionary<string, int> interned = new();

    private readonly int Index;

    private ManagedString(int index)
    {
        Index = index;
    }

    public static ManagedString Empty => default;

    public static ManagedString Create(string str)
    {
        lock (sync)
        {
            if (interned.TryGetValue(str, out var existing))
                return new ManagedString(existing);
            all.Add(str);
            var index = all.Count - 1;
            interned[str] = index;
            return new ManagedString(index);
        }
    }

    public override string ToString()
    {
        lock (sync)
            return all[Index];
    }

    public static implicit operator string(ManagedString str) =>
        str.ToString();

}
