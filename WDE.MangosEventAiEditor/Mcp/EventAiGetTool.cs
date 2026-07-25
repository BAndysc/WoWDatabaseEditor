using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Managers;
using WDE.Common.Services.Mcp;
using WDE.EventAiEditor.Data;
using WDE.EventAiEditor.Editor;
using WDE.EventAiEditor.Models;
using WDE.Module.Attributes;

namespace WDE.MangosEventAiEditor.Mcp
{
    public class EventAiGetInput
    {
        [Description("Creature entry (positive, applies to all creatures of that entry) or creature guid (negative) whose EventAI script (creature_ai_scripts) to load.")]
        public required int EntryOrGuid { get; set; }
    }

    public class EventAiGetOutput
    {
        [Description("The entry or guid the script belongs to.")]
        public required int EntryOrGuid { get; set; }

        [Description("Number of events in the script (0 = no EventAI script in the database).")]
        public required int EventCount { get; set; }

        [Description("True when the script was read from a currently open (possibly unsaved) editor document instead of the database.")]
        public required bool FromOpenDocument { get; set; }

        [Description("The script; the same shape is accepted by eventai_update and eventai_validate.")]
        public required EventAiScriptJson Script { get; set; }
    }

    [AutoRegisterToParentScope]
    [SingleInstance]
    public class EventAiGetTool : McpTool<EventAiGetInput, EventAiGetOutput>
    {
        private readonly Lazy<IEventAiFactory> factory;
        private readonly Lazy<IEventAiDataManager> dataManager;
        private readonly Lazy<IEventAiDatabaseProvider> databaseProvider;
        private readonly Lazy<IEventAiImporter> importer;
        private readonly Lazy<IDocumentManager> documentManager;

        public EventAiGetTool(Lazy<IEventAiFactory> factory,
            Lazy<IEventAiDataManager> dataManager,
            Lazy<IEventAiDatabaseProvider> databaseProvider,
            Lazy<IEventAiImporter> importer,
            Lazy<IDocumentManager> documentManager)
        {
            this.factory = factory;
            this.dataManager = dataManager;
            this.databaseProvider = databaseProvider;
            this.importer = importer;
            this.documentManager = documentManager;
        }

        public override string Name => "eventai_get";

        public override string Description => "Loads the CMaNGOS EventAI script (creature_ai_scripts) of a creature entry (positive) or guid (negative) and returns it as a structured JSON model with event/action names, parameter names and readable values. When the script is currently open in an editor document, its live (possibly unsaved) state is returned (fromOpenDocument=true); otherwise the database state is read. Does not open any editor window. The returned 'script' object round-trips with eventai_update.";

        protected override async Task<EventAiGetOutput> Execute(EventAiGetInput input, CancellationToken token)
        {
            EventAiScript script;
            var openDocument = EventAiMcpModelBuilder.FindOpenDocument(documentManager.Value, input.EntryOrGuid);
            if (openDocument != null)
                script = EventAiMcpModelBuilder.GetOpenDocumentScript(openDocument);
            else
                script = await EventAiMcpModelBuilder.LoadScriptFromDatabase(input.EntryOrGuid,
                    factory.Value, dataManager.Value, databaseProvider.Value, importer.Value);

            return new EventAiGetOutput
            {
                EntryOrGuid = input.EntryOrGuid,
                EventCount = script.Events.Count,
                FromOpenDocument = openDocument != null,
                Script = EventAiMcpModelBuilder.ToJson(script, dataManager.Value)
            };
        }
    }
}
