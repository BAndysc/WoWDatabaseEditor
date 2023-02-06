using System.Collections.Generic;

namespace WDE.DatabaseEditors.Utils;

/// <summary>
/// this is a workaround for string primary keys. DatabaseKey struct contains long only, so this
/// class is used to map string to long. It is used in DatabaseEntity.ForceGenerateKey method.
/// </summary>
internal class StringToLongMapping
{
    public static StringToLongMapping Instance { get; } = new StringToLongMapping();

    private long counter = 0;
    private Dictionary<string, long> mapping = new Dictionary<string, long>();

    public long this[string key]
    {
        get
        {
            if (!mapping.TryGetValue(key, out var value))
            {
                value = counter++;
                mapping[key] = value;
            }

            return value;
        }
    }
}