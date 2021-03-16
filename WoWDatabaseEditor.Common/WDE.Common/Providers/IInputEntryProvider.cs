using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Providers
{
    [UniqueProvider]
    public interface IInputEntryProvider
    {
        Task<uint?> GetEntry();
    }
}