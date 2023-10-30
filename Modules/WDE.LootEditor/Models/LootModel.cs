using System;
using System.Collections.Generic;
using WDE.Common.Database;

namespace WDE.LootEditor.Models;

public class LootModel
{
    public LootModel(AbstractLootEntry loot, IReadOnlyList<AbstractCondition>? conditions = null)
    {
        Loot = loot;
        Conditions = conditions ?? Array.Empty<AbstractCondition>();
    }

    public AbstractLootEntry Loot { get; }
    public IReadOnlyList<AbstractCondition> Conditions { get; }
}

public class LootGroupModel
{
    public LootGroupModel(LootSourceType type, LootEntry entry, string? name, bool dontLoadRecursively,
        params LootModel[] items)
    {
        Type = type;
        Entry = entry;
        Name = name;
        DontLoadRecursively = dontLoadRecursively;
        Items = items;
    }

    public LootSourceType Type { get; }
    public LootEntry Entry { get; }
    public string? Name { get; }
    public bool DontLoadRecursively { get; }
    public LootModel[] Items { get; } = Array.Empty<LootModel>();
}