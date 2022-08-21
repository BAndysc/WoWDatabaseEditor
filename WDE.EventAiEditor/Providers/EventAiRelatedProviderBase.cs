using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Solution;
using WDE.EventAiEditor.Models;

namespace WDE.EventAiEditor.Providers
{
    public class EventAiRelatedProviderBase<T> : ISolutionItemRelatedProvider<T> where T : IEventAiSolutionItem
    {
        public Task<RelatedSolutionItem?> GetRelated(T item)
        {
            return Task.FromResult<RelatedSolutionItem?>(
                new RelatedSolutionItem(RelatedSolutionItem.RelatedType.CreatureEntry, item.EntryOrGuid));
        }
    }
}