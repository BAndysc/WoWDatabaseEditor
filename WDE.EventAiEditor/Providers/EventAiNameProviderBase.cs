using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.Solution;
using WDE.EventAiEditor.Models;

namespace WDE.EventAiEditor.Providers
{
    public class EventAiNameProviderBase<T> : ISolutionNameProvider<T> where T : IEventAiSolutionItem
    {
        private readonly ICachedDatabaseProvider database;

        public EventAiNameProviderBase(ICachedDatabaseProvider database)
        {
            this.database = database;
        }
        
        private string? TryGetName(int entryOrGuid)
        {
            uint? entry = 0;
            if (entryOrGuid < 0)
                entry = database.GetCachedCreatureByGuid(0, (uint)-entryOrGuid)?.Entry;
            else
                entry = (uint)entryOrGuid;
                    
            if (entry.HasValue)
                return database.GetCachedCreatureTemplate(entry.Value)?.Name;

            return null;
        }

        public virtual string GetName(T item)
        {
            var name = TryGetName(item.EntryOrGuid);
            if (!string.IsNullOrEmpty(name))
            {
                if (item.EntryOrGuid < 0)
                    return name + " with guid " + -item.EntryOrGuid;
                return name;
            }
            
            int entry = item.EntryOrGuid;

            if (entry > 0)
                return "Creature " + entry;

            return "Creature with guid " + -entry;
        }
    }
}