using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Services.HeadConnector
{
    [UniqueProvider]
    public interface IHeadFetchService
    {
        Task<IList<Ability>> FetchNpcAbilities(HeadSourceType source, uint entry);
    }
}