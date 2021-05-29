using Prism.Events;

namespace WDE.Common.Events
{
    public class EventRequestGenerateSqlArgs
    {
        public EventRequestGenerateSqlArgs(ISolutionItem item, string? sql)
        {
            Item = item;
            Sql = sql;
        }

        public ISolutionItem Item { get; set; }
        public string? Sql { get; set; }
    }

    public class EventRequestGenerateSql : PubSubEvent<EventRequestGenerateSqlArgs>
    {
    }
}