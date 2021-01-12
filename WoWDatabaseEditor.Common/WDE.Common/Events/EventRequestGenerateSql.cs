using Prism.Events;

namespace WDE.Common.Events
{
    public class EventRequestGenerateSqlArgs
    {
        public ISolutionItem Item { get; set; }
        public string Sql { get; set; }
    }

    public class EventRequestGenerateSql : PubSubEvent<EventRequestGenerateSqlArgs>
    {
    }
}