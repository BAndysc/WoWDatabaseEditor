using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Solution
{
    [NonUniqueProvider]
    public interface ISolutionItemRelatedProvider
    {
        
    }
    
    public interface ISolutionItemRelatedProvider<T> : ISolutionItemRelatedProvider where T : ISolutionItem
    {
        Task<RelatedSolutionItem?> GetRelated(T item);
    }
    
    public readonly struct RelatedSolutionItem
    {
        public long Entry { get; }
        public RelatedType Type { get; }

        public RelatedSolutionItem(RelatedType type, long entry)
        {
            Entry = entry;
            Type = type;
        }
        
        public enum RelatedType
        {
            CreatureEntry,
            GameobjectEntry,
            GossipMenu,
            QuestEntry
        }
    }
    
    [UniqueProvider]
    public interface ISolutionItemRelatedRegistry
    {
        Task<RelatedSolutionItem?> GetRelated(ISolutionItem item);
    }
}