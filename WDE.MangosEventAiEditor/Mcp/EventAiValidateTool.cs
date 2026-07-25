using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Managers;
using WDE.Common.Services.Mcp;
using WDE.EventAiEditor.Data;
using WDE.EventAiEditor.Editor;
using WDE.EventAiEditor.Inspections;
using WDE.EventAiEditor.Models;
using WDE.Module.Attributes;

namespace WDE.MangosEventAiEditor.Mcp
{
    public class EventAiValidateInput
    {
        [Description("Creature entry (positive) or creature guid (negative) the script belongs to.")]
        public required int EntryOrGuid { get; set; }

        [Description("Optional script to validate (same shape as returned by eventai_get). When omitted, the current database state of creature_ai_scripts for entryOrGuid is validated.")]
        public EventAiScriptJson? Script { get; set; }
    }

    public class EventAiValidateOutput
    {
        [Description("Number of validated events.")]
        public required int EventCount { get; set; }

        [Description("True when the validated script came from a currently open (possibly unsaved) editor document instead of the database.")]
        public required bool FromOpenDocument { get; set; }

        [Description("Found problems; an empty list means the script passed all inspections.")]
        public required List<EventAiProblemJson> Problems { get; set; }
    }

    [AutoRegisterToParentScope]
    [SingleInstance]
    public class EventAiValidateTool : McpTool<EventAiValidateInput, EventAiValidateOutput>
    {
        private readonly Lazy<IEventAiFactory> factory;
        private readonly Lazy<IEventAiDataManager> dataManager;
        private readonly Lazy<IEventAiDatabaseProvider> databaseProvider;
        private readonly Lazy<IEventAiImporter> importer;
        private readonly Lazy<IEventAiInspectorService> inspectorService;
        private readonly Lazy<IDocumentManager> documentManager;

        public EventAiValidateTool(Lazy<IEventAiFactory> factory,
            Lazy<IEventAiDataManager> dataManager,
            Lazy<IEventAiDatabaseProvider> databaseProvider,
            Lazy<IEventAiImporter> importer,
            Lazy<IEventAiInspectorService> inspectorService,
            Lazy<IDocumentManager> documentManager)
        {
            this.factory = factory;
            this.dataManager = dataManager;
            this.databaseProvider = databaseProvider;
            this.importer = importer;
            this.inspectorService = inspectorService;
            this.documentManager = documentManager;
        }

        public override string Name => "eventai_validate";

        public override string Description => "Runs the editor's EventAI inspections (required parameters, value ranges, duplicate events, per-event rules...) over a script and returns the list of problems. Validates, in order of preference: the 'script' JSON when provided (same shape as eventai_get returns, nothing is touched), else the live state of a currently open editor document for entryOrGuid (fromOpenDocument=true), else the current database state of creature_ai_scripts.";

        protected override async Task<EventAiValidateOutput> Execute(EventAiValidateInput input, CancellationToken token)
        {
            EventAiScript script;
            bool fromOpenDocument = false;
            if (input.Script != null)
                script = EventAiMcpModelBuilder.FromJson(input.EntryOrGuid, input.Script, factory.Value, dataManager.Value);
            else
            {
                var openDocument = EventAiMcpModelBuilder.FindOpenDocument(documentManager.Value, input.EntryOrGuid);
                if (openDocument != null)
                {
                    script = EventAiMcpModelBuilder.GetOpenDocumentScript(openDocument);
                    fromOpenDocument = true;
                }
                else
                    script = await EventAiMcpModelBuilder.LoadScriptFromDatabase(input.EntryOrGuid,
                        factory.Value, dataManager.Value, databaseProvider.Value, importer.Value);
            }

            var inspections = inspectorService.Value.GenerateInspections(script);
            return new EventAiValidateOutput
            {
                EventCount = script.Events.Count,
                FromOpenDocument = fromOpenDocument,
                Problems = EventAiMcpModelBuilder.ToProblems(inspections)
            };
        }
    }
}
