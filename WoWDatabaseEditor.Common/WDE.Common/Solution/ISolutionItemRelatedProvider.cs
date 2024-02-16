using System;
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
    
    public readonly struct RelatedSolutionItem : IEquatable<RelatedSolutionItem>
    {
        public bool Equals(RelatedSolutionItem other)
        {
            return Entry == other.Entry && Type == other.Type;
        }

        public override bool Equals(object? obj)
        {
            return obj is RelatedSolutionItem other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Entry, (int)Type);
        }

        public static bool operator ==(RelatedSolutionItem left, RelatedSolutionItem right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RelatedSolutionItem left, RelatedSolutionItem right)
        {
            return !left.Equals(right);
        }

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
            QuestEntry,
            Template,
            TimedActionList,
            Spell
        }
    }
    
    [UniqueProvider]
    public interface ISolutionItemRelatedRegistry
    {
        Task<RelatedSolutionItem?> GetRelated(ISolutionItem item);
    }
}