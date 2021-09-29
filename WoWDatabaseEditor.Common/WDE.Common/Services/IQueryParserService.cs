using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Services
{
    [UniqueProvider]
    public interface IQueryParserService
    {
        public Task<(IList<ISolutionItem> items, IList<string> errors)> GenerateItemsForQuery(string query);
    }
}