using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common
{
    [UniqueProvider]
    public interface ICreatureEntryOrGuidProviderService
    {
        Task<int?> GetEntryFromService();
    }

    [UniqueProvider]
    public interface IGameobjectEntryOrGuidProviderService
    {
        Task<int?> GetEntryFromService();
    }

    [UniqueProvider]
    public interface IQuestEntryProviderService
    {
        Task<uint?> GetEntryFromService(uint? questId = null);
    }

    [UniqueProvider]
    public interface ISpellEntryProviderService
    {
        Task<uint?> GetEntryFromService(uint? spellId = null);
    }
}