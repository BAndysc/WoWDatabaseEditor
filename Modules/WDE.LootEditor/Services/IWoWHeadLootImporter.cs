using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WDE.LootEditor.Services;

[UniqueProvider]
public interface IWoWHeadLootImporter
{
    bool IsAvailable { get; }
    Task<IList<AbstractLootEntry>> GetCreatureLoot(uint entry, bool onlyQuestItems);
    Task<IList<AbstractLootEntry>> GetGameObjectLoot(uint entry, bool onlyQuestItems);
}

[FallbackAutoRegister]
public class WoWHeadLootImporter : IWoWHeadLootImporter
{
    public bool IsAvailable => false;

    public Task<IList<AbstractLootEntry>> GetCreatureLoot(uint entry, bool onlyQuestItems)
    {
        // todo
        throw new NotImplementedException();
    }
    
    public Task<IList<AbstractLootEntry>> GetGameObjectLoot(uint entry, bool onlyQuestItems)
    {
        // todo
        throw new NotImplementedException();
    }
}

public class WowHeadImportException : Exception
{
    public WowHeadImportException(string error) : base(error)
    {
    }
}