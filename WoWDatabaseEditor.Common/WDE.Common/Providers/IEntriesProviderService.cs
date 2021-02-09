using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common
{
    [UniqueProvider]
    public interface ICreatureEntryProviderService
    {
        Task<uint?> GetEntryFromService();
    }

    [UniqueProvider]
    public interface IGameobjectEntryProviderService
    {
        Task<uint?> GetEntryFromService();
    }

    [UniqueProvider]
    public interface IQuestEntryProviderService
    {
        Task<uint?> GetEntryFromService();
    }

    [UniqueProvider]
    public interface ISpellEntryProviderService
    {
        Task<uint?> GetEntryFromService();
    }
}