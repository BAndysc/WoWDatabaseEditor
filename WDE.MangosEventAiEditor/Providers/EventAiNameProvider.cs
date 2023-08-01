using WDE.Common.Database;
using WDE.EventAiEditor;
using WDE.EventAiEditor.Providers;
using WDE.Module.Attributes;

namespace WDE.MangosEventAiEditor.Providers
{
    [AutoRegisterToParentScope]
    public class EventAiNameProvider : EventAiNameProviderBase<EventAiSolutionItem>
    {
        public EventAiNameProvider(ICachedDatabaseProvider database) : base(database)
        {
        }
        
        public override string GetName(EventAiSolutionItem item)
        {
            var name = base.GetName(item);
            return name + " ai script";
        }
    }
}