using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common
{
    [UniqueProvider]
    public interface ICreatureEntryOrGuidProviderService
    {
        Task<int?> GetEntryFromService(uint? entry = null, string? customCounterTable = null);
        Task<IReadOnlyCollection<int>> GetEntriesFromService(string? customCounterTable = null);
    }

    [UniqueProvider]
    public interface IGameobjectEntryOrGuidProviderService
    {
        Task<int?> GetEntryFromService(uint? entry = null, string? customCounterTable = null);
        Task<IReadOnlyCollection<int>> GetEntriesFromService(string? customCounterTable = null);
    }

    [UniqueProvider]
    public interface IQuestEntryProviderService
    {
        Task<uint?> GetEntryFromService(uint? questId = null);
        Task<IReadOnlyCollection<uint>> GetEntriesFromService();
    }

    [UniqueProvider]
    public interface ISpellEntryProviderService
    {
        Task<uint?> GetEntryFromService(uint? spellId = null, string? customCounterTable = null);
        Task<IReadOnlyCollection<uint>> GetEntriesFromService(string? customCounterTable = null);
    }
}