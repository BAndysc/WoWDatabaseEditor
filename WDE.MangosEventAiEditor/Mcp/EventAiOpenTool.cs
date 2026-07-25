using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Prism.Events;
using WDE.Common.Events;
using WDE.Common.Services.Mcp;
using WDE.EventAiEditor;
using WDE.Module.Attributes;

namespace WDE.MangosEventAiEditor.Mcp
{
    public class EventAiOpenInput
    {
        [Description("Creature entry (positive) or creature guid (negative) whose EventAI script to open in the editor.")]
        public required int EntryOrGuid { get; set; }
    }

    public class EventAiOpenOutput
    {
        [Description("The entry or guid the opened editor is bound to.")]
        public required int EntryOrGuid { get; set; }

        [Description("Human readable confirmation.")]
        public required string Message { get; set; }
    }

    [AutoRegisterToParentScope]
    [SingleInstance]
    public class EventAiOpenTool : McpTool<EventAiOpenInput, EventAiOpenOutput>
    {
        private readonly IEventAggregator eventAggregator;

        public EventAiOpenTool(IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;
        }

        public override string Name => "eventai_open";

        public override string Description => "Opens the EventAI editor document for the given creature entry (positive) or guid (negative) in the WoW Database Editor, so the user can see and edit the script (creature_ai_scripts) in the UI.";

        public override bool Mutating => true;

        protected override Task<EventAiOpenOutput> Execute(EventAiOpenInput input, CancellationToken token)
        {
            eventAggregator.GetEvent<EventRequestOpenItem>().Publish(new EventAiSolutionItem(input.EntryOrGuid));
            return Task.FromResult(new EventAiOpenOutput
            {
                EntryOrGuid = input.EntryOrGuid,
                Message = $"Opened the EventAI editor for {(input.EntryOrGuid < 0 ? "creature guid " + (-input.EntryOrGuid) : "creature entry " + input.EntryOrGuid)}."
            });
        }
    }
}
