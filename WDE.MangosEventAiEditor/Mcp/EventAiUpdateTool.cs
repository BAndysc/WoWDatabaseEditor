using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Services.Mcp;
using WDE.EventAiEditor;
using WDE.EventAiEditor.Data;
using WDE.EventAiEditor.Editor.UserControls;
using WDE.EventAiEditor.Inspections;
using WDE.Module.Attributes;

namespace WDE.MangosEventAiEditor.Mcp
{
    public class EventAiUpdateInput
    {
        [Description("Creature entry (positive) or creature guid (negative) whose EventAI script to replace.")]
        public required int EntryOrGuid { get; set; }

        [Description("The full new script (same shape as returned by eventai_get). It REPLACES the whole creature_ai_scripts script of this entry/guid; an empty events list deletes the script.")]
        public required EventAiScriptJson Script { get; set; }

        [Description("When true, the generated SQL is executed against the world database. When false (default), the SQL is only returned for review. Not allowed while the document is open in the editor - the changes are applied to the open document instead and the user saves them.")]
        public bool Execute { get; set; }
    }

    public class EventAiUpdateOutput
    {
        [Description("The generated SQL replacing the creature_ai_scripts script. Null when the changes were applied to an open editor document instead.")]
        public string? Sql { get; set; }

        [Description("True when the SQL was executed against the database.")]
        public required bool Executed { get; set; }

        [Description("True when the changes were applied to the live model of a currently open editor document (as one undoable step) instead of generating SQL.")]
        public required bool AppliedToOpenDocument { get; set; }

        [Description("Extra information about what happened and what to do next.")]
        public string? Note { get; set; }

        [Description("Inspection problems found in the new script (informational, they do not block generation/execution).")]
        public required List<EventAiProblemJson> Problems { get; set; }
    }

    [AutoRegisterToParentScope]
    [SingleInstance]
    public class EventAiUpdateTool : McpTool<EventAiUpdateInput, EventAiUpdateOutput>
    {
        private readonly Lazy<IEventAiFactory> factory;
        private readonly Lazy<IEventAiDataManager> dataManager;
        private readonly Lazy<IEventAiExporter> exporter;
        private readonly Lazy<IEventAiInspectorService> inspectorService;
        private readonly Lazy<IMySqlExecutor> mySqlExecutor;
        private readonly Lazy<IDocumentManager> documentManager;

        public EventAiUpdateTool(Lazy<IEventAiFactory> factory,
            Lazy<IEventAiDataManager> dataManager,
            Lazy<IEventAiExporter> exporter,
            Lazy<IEventAiInspectorService> inspectorService,
            Lazy<IMySqlExecutor> mySqlExecutor,
            Lazy<IDocumentManager> documentManager)
        {
            this.factory = factory;
            this.dataManager = dataManager;
            this.exporter = exporter;
            this.inspectorService = inspectorService;
            this.mySqlExecutor = mySqlExecutor;
            this.documentManager = documentManager;
        }

        public override string Name => "eventai_update";

        public override string Description => "Replaces the whole CMaNGOS EventAI script of a creature with the given JSON (same shape as eventai_get returns) and runs the editor inspections over it. If the script is currently OPEN in an editor document, the change is applied to the live document as one undoable edit (the user reviews it and saves, no SQL is executed). Otherwise the replacing creature_ai_scripts SQL is generated and, with execute=true, run against the world database. Tip: call eventai_get first and modify its output.";

        public override bool Mutating => true;

        protected override async Task<EventAiUpdateOutput> Execute(EventAiUpdateInput input, CancellationToken token)
        {
            if (input.Script == null!)
                throw new McpToolException("Missing required 'script' object.");

            var openDocument = EventAiMcpModelBuilder.FindOpenDocument(documentManager.Value, input.EntryOrGuid);
            if (openDocument != null)
            {
                if (input.Execute)
                    throw new McpToolException($"The EventAI document for {input.EntryOrGuid} is currently open in the editor, so nothing was applied. Retry with execute=false to apply the changes to the open document (the user can then review and save via document_save or Ctrl+S), or ask the user to close the document first.");

                var openScript = EventAiMcpModelBuilder.GetOpenDocumentScript(openDocument);
                // build (and validate) all events BEFORE touching the document, so invalid input leaves it untouched
                var newEvents = EventAiMcpModelBuilder.BuildEvents(input.Script, factory.Value);
                EventAiMcpModelBuilder.ReplaceScriptEvents(openScript, newEvents);

                return new EventAiUpdateOutput
                {
                    Sql = null,
                    Executed = false,
                    AppliedToOpenDocument = true,
                    Note = "The changes were applied to the open EventAI editor document as a single undoable edit. No SQL was executed - the user can review them and save the document (document_save or Ctrl+S), or undo them.",
                    Problems = EventAiMcpModelBuilder.ToProblems(inspectorService.Value.GenerateInspections(openScript))
                };
            }

            var script = EventAiMcpModelBuilder.FromJson(input.EntryOrGuid, input.Script, factory.Value, dataManager.Value);
            var problems = EventAiMcpModelBuilder.ToProblems(inspectorService.Value.GenerateInspections(script));

            var item = new EventAiSolutionItem(input.EntryOrGuid);
            var query = await exporter.Value.GenerateSql(item, script);

            bool executed = false;
            if (input.Execute)
            {
                try
                {
                    await mySqlExecutor.Value.ExecuteSql(query);
                    executed = true;
                }
                catch (IMySqlExecutor.DatabaseExecutorException e)
                {
                    throw new McpToolException("Failed to execute the query: " + (e.InnerException?.Message ?? e.Message));
                }
            }

            return new EventAiUpdateOutput
            {
                Sql = query.QueryString,
                Executed = executed,
                AppliedToOpenDocument = false,
                Note = executed ? null : "The SQL was only generated, not executed. Pass execute=true to run it against the world database.",
                Problems = problems
            };
        }
    }
}
