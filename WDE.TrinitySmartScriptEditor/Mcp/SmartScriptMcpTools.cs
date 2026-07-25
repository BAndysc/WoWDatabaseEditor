using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Prism.Events;
using WDE.Common.Database;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Services.Mcp;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Editor;
using WDE.SmartScriptEditor.Editor.ViewModels;
using WDE.SmartScriptEditor.Inspections;
using WDE.SmartScriptEditor.Models;
using WDE.SmartScriptEditor.Services;

namespace WDE.TrinitySmartScriptEditor.Mcp;

// ---------- smart_data_search ----------

public enum SmartDataKind
{
    Event,
    Action,
    Target
}

public sealed class SmartDataSearchInput
{
    [Description("Which kind of smart data to search: Event, Action or Target (targets double as action sources)")]
    public required SmartDataKind Type { get; init; }

    [Description("Case-insensitive regex (invalid regex falls back to substring match) over the internal name (SMART_EVENT_...) and the readable name")]
    public string? Filter { get; init; }

    [Description("Exact id to look up")]
    public int? Id { get; init; }

    [Description("Include deprecated entries (default false)")]
    public bool IncludeDeprecated { get; init; }

    [Description("Maximum number of results (default 100)")]
    public int Limit { get; init; } = 100;
}

public sealed class SmartDataParameterInfo
{
    public required string Name { get; init; }

    [Description("Parameter value type; enum/flag parameters also list their known values")]
    public required string Type { get; init; }

    public string? Description { get; init; }
    public bool Required { get; init; }
    public long DefaultValue { get; init; }

    [Description("value -> meaning map for enum/flag parameters")]
    public Dictionary<long, string>? Values { get; init; }
}

public sealed class SmartDataEntry
{
    public required int Id { get; init; }

    [Description("Internal name, i.e. SMART_EVENT_UPDATE_IC; usable instead of id in smart_script_update")]
    public required string Name { get; init; }

    public required string ReadableName { get; init; }
    public string? Help { get; init; }
    public bool Deprecated { get; init; }
    public required List<SmartDataParameterInfo> Parameters { get; init; }

    [Description("Script types this entry can be used with; null = any")]
    public List<string>? UsableWithScriptTypes { get; init; }
}

[AutoRegisterToParentScope]
[SingleInstance]
public class SmartDataSearchTool : McpTool<SmartDataSearchInput, List<SmartDataEntry>>
{
    private readonly Lazy<ISmartDataManager> smartDataManager;

    public SmartDataSearchTool(Lazy<ISmartDataManager> smartDataManager)
    {
        this.smartDataManager = smartDataManager;
    }

    public override string Name => "smart_data_search";
    public override string Description => "Searches the SmartScript (smart AI) definitions: available events, actions and targets/sources with their ids, names, parameter meanings (including enum value maps) and supported script types. Use this to learn what building blocks exist before reading or writing scripts with the smart_script_* tools.";

    protected override Task<List<SmartDataEntry>> Execute(SmartDataSearchInput input, CancellationToken token)
    {
        var smartType = input.Type switch
        {
            SmartDataKind.Event => SmartType.SmartEvent,
            SmartDataKind.Action => SmartType.SmartAction,
            SmartDataKind.Target => SmartType.SmartTarget,
            _ => throw new McpToolException($"Unknown data type {input.Type}")
        };

        var all = smartDataManager.Value.GetAllData(smartType).Value;

        Func<SmartGenericJsonData, bool> matches = _ => true;
        var filter = input.Filter;
        if (!string.IsNullOrEmpty(filter))
        {
            Regex? regex = null;
            try
            {
                regex = new Regex(filter, RegexOptions.IgnoreCase);
            }
            catch (ArgumentException)
            {
                // not a valid regex - fall back to substring matching
            }

            if (regex != null)
                matches = d => regex.IsMatch(d.Name) || regex.IsMatch(d.NameReadable);
            else
                matches = d => d.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                               d.NameReadable.Contains(filter, StringComparison.OrdinalIgnoreCase);
        }

        var result = all
            .Where(d => !input.Id.HasValue || d.Id == input.Id.Value)
            .Where(d => input.IncludeDeprecated || !d.Deprecated)
            .Where(matches)
            .OrderBy(d => d.Id)
            .Take(Math.Max(1, input.Limit))
            .Select(d => new SmartDataEntry
            {
                Id = d.Id,
                Name = d.Name,
                ReadableName = d.NameReadable,
                Help = string.IsNullOrEmpty(d.Help) ? null : d.Help,
                Deprecated = d.Deprecated,
                Parameters = d.Parameters == null
                    ? new List<SmartDataParameterInfo>()
                    : d.Parameters.Select(p => new SmartDataParameterInfo
                    {
                        Name = p.Name,
                        Type = p.Type,
                        Description = string.IsNullOrEmpty(p.Description) ? null : p.Description,
                        Required = p.Required,
                        DefaultValue = p.DefaultVal,
                        Values = p.Values == null || p.Values.Count == 0
                            ? null
                            : p.Values.ToDictionary(
                                kvp => kvp.Key,
                                kvp => string.IsNullOrEmpty(kvp.Value.Description)
                                    ? kvp.Value.Name
                                    : $"{kvp.Value.Name}: {kvp.Value.Description}")
                    }).ToList(),
                UsableWithScriptTypes = ScriptTypeNames(d.UsableWithScriptTypes)
            })
            .ToList();

        return Task.FromResult(result);
    }

    private static List<string>? ScriptTypeNames(SmartScriptTypeMask? mask)
    {
        if (!mask.HasValue)
            return null;
        var list = new List<string>();
        foreach (var type in Enum.GetValues<SmartScriptType>())
        {
            if (type == SmartScriptType.END)
                continue;
            if (mask.Value.HasFlagFast(type.ToMask()) && !list.Contains(type.ToString()))
                list.Add(type.ToString());
        }
        return list;
    }
}

// ---------- smart_script_get ----------

public sealed class SmartScriptRefInput
{
    [Description("Entry (positive) of the creature/gameobject/quest/... or negative guid for a per-spawn script")]
    public required int EntryOrGuid { get; init; }

    [Description("Script type: Creature, GameObject, Quest, Spell, TimedActionList, AreaTrigger, Event, Gossip, Transport, Instance, Scene, ...")]
    public required string ScriptType { get; init; }
}

[AutoRegisterToParentScope]
[SingleInstance]
public class SmartScriptGetTool : McpTool<SmartScriptRefInput, McpScript>
{
    private readonly Lazy<ISmartScriptDatabaseProvider> database;
    private readonly Lazy<ISmartFactory> smartFactory;
    private readonly Lazy<ISmartDataManager> smartDataManager;
    private readonly Lazy<IEditorFeatures> editorFeatures;
    private readonly Lazy<ISmartScriptImporter> importer;
    private readonly Lazy<IDocumentManager> documentManager;

    public SmartScriptGetTool(Lazy<ISmartScriptDatabaseProvider> database,
        Lazy<ISmartFactory> smartFactory,
        Lazy<ISmartDataManager> smartDataManager,
        Lazy<IEditorFeatures> editorFeatures,
        Lazy<ISmartScriptImporter> importer,
        Lazy<IDocumentManager> documentManager)
    {
        this.database = database;
        this.smartFactory = smartFactory;
        this.smartDataManager = smartDataManager;
        this.editorFeatures = editorFeatures;
        this.importer = importer;
        this.documentManager = documentManager;
    }

    public override string Name => "smart_script_get";
    public override string Description => "Reads a SmartScript (smart AI) and returns it as structured JSON: events (with chance/flags/phases/cooldowns and named parameter values) and their actions (with parameters, source and target). If the script's document is currently open in the editor, the live (possibly unsaved) state is returned and fromOpenDocument is true; otherwise the script is loaded from the database without opening any window. The returned object round-trips with smart_script_update's 'script' input. Note: conditions attached to events or targets are not included, and editor-only group headers are omitted.";

    protected override async Task<McpScript> Execute(SmartScriptRefInput input, CancellationToken token)
    {
        var type = SmartScriptMcpModel.ParseScriptType(input.ScriptType);

        var openDocument = SmartScriptMcpModel.FindOpenDocument(documentManager.Value, input.EntryOrGuid, type);
        if (openDocument != null)
            return SmartScriptMcpModel.ToJsonModel(openDocument.Script, smartDataManager.Value, editorFeatures.Value, fromOpenDocument: true);

        var (_, script) = await SmartScriptMcpModel.LoadFromDatabase(input.EntryOrGuid, type,
            database.Value, smartFactory.Value, smartDataManager.Value, editorFeatures.Value, importer.Value, throwIfEmpty: true);
        return SmartScriptMcpModel.ToJsonModel(script, smartDataManager.Value, editorFeatures.Value);
    }
}

// ---------- smart_script_validate ----------

public sealed class SmartScriptValidateInput
{
    [Description("Entry (positive) or negative guid the script belongs to")]
    public required int EntryOrGuid { get; init; }

    [Description("Script type: Creature, GameObject, Quest, Spell, TimedActionList, ...")]
    public required string ScriptType { get; init; }

    [Description("Script to validate (same shape as smart_script_update's 'script'). When omitted, the open editor document (if any) or the script currently stored in the database is validated instead.")]
    public McpScriptInput? Script { get; init; }
}

public sealed class SmartScriptValidateOutput
{
    [Description("True when the validated script was the live (possibly unsaved) state of a document currently open in the editor")]
    public bool FromOpenDocument { get; init; }

    public required List<McpProblem> Problems { get; init; }
}

[AutoRegisterToParentScope]
[SingleInstance]
public class SmartScriptValidateTool : McpTool<SmartScriptValidateInput, SmartScriptValidateOutput>
{
    private readonly Lazy<ISmartScriptDatabaseProvider> database;
    private readonly Lazy<ISmartFactory> smartFactory;
    private readonly Lazy<ISmartDataManager> smartDataManager;
    private readonly Lazy<IEditorFeatures> editorFeatures;
    private readonly Lazy<ISmartScriptImporter> importer;
    private readonly Lazy<ISmartScriptInspectorService> inspectorService;
    private readonly Lazy<IDocumentManager> documentManager;

    public SmartScriptValidateTool(Lazy<ISmartScriptDatabaseProvider> database,
        Lazy<ISmartFactory> smartFactory,
        Lazy<ISmartDataManager> smartDataManager,
        Lazy<IEditorFeatures> editorFeatures,
        Lazy<ISmartScriptImporter> importer,
        Lazy<ISmartScriptInspectorService> inspectorService,
        Lazy<IDocumentManager> documentManager)
    {
        this.database = database;
        this.smartFactory = smartFactory;
        this.smartDataManager = smartDataManager;
        this.editorFeatures = editorFeatures;
        this.importer = importer;
        this.inspectorService = inspectorService;
        this.documentManager = documentManager;
    }

    public override string Name => "smart_script_validate";
    public override string Description => "Runs the editor's SmartScript inspections (missing required parameters, out of range values, structural problems...) and returns problems with severity and line number. Validates, in order of precedence: a provided script JSON (same shape as smart_script_update's 'script'); otherwise, when the script's document is open in the editor, its live (possibly unsaved) state; otherwise the script stored in the database.";

    protected override async Task<SmartScriptValidateOutput> Execute(SmartScriptValidateInput input, CancellationToken token)
    {
        var type = SmartScriptMcpModel.ParseScriptType(input.ScriptType);

        SmartScript script;
        bool fromOpenDocument = false;
        if (input.Script != null)
        {
            (_, script) = SmartScriptMcpModel.BuildFromJsonModel(input.EntryOrGuid, type, input.Script,
                smartFactory.Value, smartDataManager.Value, editorFeatures.Value, importer.Value);
        }
        else if (SmartScriptMcpModel.FindOpenDocument(documentManager.Value, input.EntryOrGuid, type) is { } openDocument)
        {
            script = openDocument.Script;
            fromOpenDocument = true;
        }
        else
        {
            (_, script) = await SmartScriptMcpModel.LoadFromDatabase(input.EntryOrGuid, type,
                database.Value, smartFactory.Value, smartDataManager.Value, editorFeatures.Value, importer.Value, throwIfEmpty: true);
        }

        return new SmartScriptValidateOutput
        {
            FromOpenDocument = fromOpenDocument,
            Problems = inspectorService.Value.GenerateInspections(script)
                .Select(r => new McpProblem { Severity = r.Severity.ToString(), Message = r.Message, Line = r.Line })
                .ToList()
        };
    }
}

// ---------- smart_script_update ----------

public sealed class SmartScriptUpdateInput
{
    [Description("Entry (positive) or negative guid the script belongs to")]
    public required int EntryOrGuid { get; init; }

    [Description("Script type: Creature, GameObject, Quest, Spell, TimedActionList, ...")]
    public required string ScriptType { get; init; }

    [Description("The full script (events with actions); it replaces the currently saved script of this entry. Use smart_script_get to fetch the current shape first when editing.")]
    public required McpScriptInput Script { get; init; }

    [Description("When true, the generated SQL is also executed against the database. Default: false = only return the SQL for review. Not allowed while the script's document is open in the editor.")]
    public bool Execute { get; init; }
}

public sealed class SmartScriptUpdateOutput
{
    [Description("Generated SQL; null when the change was applied to an open editor document instead")]
    public string? Sql { get; init; }

    public required bool Executed { get; init; }

    [Description("True when the change was applied to the live open editor document (visible to the user, undoable, not yet saved)")]
    public bool AppliedToOpenDocument { get; init; }

    public string? Message { get; init; }

    [Description("Inspection problems found in the new script (also available via smart_script_validate)")]
    public required List<McpProblem> Problems { get; init; }
}

[AutoRegisterToParentScope]
[SingleInstance]
public class SmartScriptUpdateTool : McpTool<SmartScriptUpdateInput, SmartScriptUpdateOutput>
{
    private readonly Lazy<ISmartFactory> smartFactory;
    private readonly Lazy<ISmartDataManager> smartDataManager;
    private readonly Lazy<IEditorFeatures> editorFeatures;
    private readonly Lazy<ISmartScriptImporter> importer;
    private readonly Lazy<ISmartScriptExporter> exporter;
    private readonly Lazy<ISmartScriptInspectorService> inspectorService;
    private readonly Lazy<IMySqlExecutor> sqlExecutor;
    private readonly Lazy<IDocumentManager> documentManager;

    public SmartScriptUpdateTool(Lazy<ISmartFactory> smartFactory,
        Lazy<ISmartDataManager> smartDataManager,
        Lazy<IEditorFeatures> editorFeatures,
        Lazy<ISmartScriptImporter> importer,
        Lazy<ISmartScriptExporter> exporter,
        Lazy<ISmartScriptInspectorService> inspectorService,
        Lazy<IMySqlExecutor> sqlExecutor,
        Lazy<IDocumentManager> documentManager)
    {
        this.smartFactory = smartFactory;
        this.smartDataManager = smartDataManager;
        this.editorFeatures = editorFeatures;
        this.importer = importer;
        this.exporter = exporter;
        this.inspectorService = inspectorService;
        this.sqlExecutor = sqlExecutor;
        this.documentManager = documentManager;
    }

    public override string Name => "smart_script_update";
    public override string Description => "Writes a SmartScript from the given JSON (shape of smart_script_get: events with parameters and actions with source/target); the script is fully replaced. If the script's document is currently OPEN in the editor, the change is applied to the live document instead of the database: the user sees it immediately, can undo it as a single step and saves it themselves (document_save tool or Ctrl+S); execute=true is rejected in that case. Otherwise the same SQL the editor's Save would produce is generated; by default it is only returned for review plus inspection problems - pass execute=true to apply it. Conditions attached to events are not covered and will be removed on save if the script had any.";
    public override bool Mutating => true;

    protected override async Task<SmartScriptUpdateOutput> Execute(SmartScriptUpdateInput input, CancellationToken token)
    {
        var type = SmartScriptMcpModel.ParseScriptType(input.ScriptType);

        var openDocument = SmartScriptMcpModel.FindOpenDocument(documentManager.Value, input.EntryOrGuid, type);
        if (openDocument != null)
            return ApplyToOpenDocument(openDocument, input, type);

        var (item, script) = SmartScriptMcpModel.BuildFromJsonModel(input.EntryOrGuid, type, input.Script,
            smartFactory.Value, smartDataManager.Value, editorFeatures.Value, importer.Value);

        var problems = Inspect(script);
        var query = await exporter.Value.GenerateSql(item, script);

        if (input.Execute)
        {
            try
            {
                await sqlExecutor.Value.ExecuteSql(query);
            }
            catch (Exception e)
            {
                throw new McpToolException($"Failed to execute the SQL: {e.Message}");
            }
        }

        return new SmartScriptUpdateOutput
        {
            Sql = query.QueryString,
            Executed = input.Execute,
            Problems = problems
        };
    }

    private SmartScriptUpdateOutput ApplyToOpenDocument(SmartScriptEditorViewModel openDocument, SmartScriptUpdateInput input, SmartScriptType type)
    {
        var what = SmartScriptMcpModel.DescribeEntry(input.EntryOrGuid, type);
        if (input.Execute)
            throw new McpToolException($"The SmartScript document for {what} is currently open in the editor, so execute=true is not allowed and NO changes were applied. Call again with execute=false to modify the open document; the user can then review the change and save it with document_save (or Ctrl+S).");

        var openScript = openDocument.Script;

        // build (and validate) all new events first, then swap the content atomically as one undo step
        var newEvents = SmartScriptMcpModel.BuildEvents(openScript, input.Script, smartFactory.Value, smartDataManager.Value);

        using (openScript.BulkEdit("MCP smart_script_update"))
        {
            // no Events.Clear(): the history handler only understands per-item Add/Remove, not Reset
            for (int i = openScript.Events.Count - 1; i >= 0; --i)
                openScript.Events.RemoveAt(i);
            foreach (var ev in newEvents)
                openScript.Events.Add(ev);
        }

        return new SmartScriptUpdateOutput
        {
            Sql = null,
            Executed = false,
            AppliedToOpenDocument = true,
            Message = $"Applied the changes to the open editor document for {what}. The user can review them (undo works as a single step) and save with document_save or Ctrl+S; nothing has been written to the database yet.",
            Problems = Inspect(openScript)
        };
    }

    private List<McpProblem> Inspect(SmartScript script)
        => inspectorService.Value.GenerateInspections(script)
            .Select(r => new McpProblem { Severity = r.Severity.ToString(), Message = r.Message, Line = r.Line })
            .ToList();
}

// ---------- smart_script_open ----------

[AutoRegisterToParentScope]
[SingleInstance]
public class SmartScriptOpenTool : McpTool<SmartScriptRefInput, string>
{
    private readonly Lazy<ISmartScriptFactory> smartScriptFactory;
    private readonly IEventAggregator eventAggregator;

    public SmartScriptOpenTool(Lazy<ISmartScriptFactory> smartScriptFactory,
        IEventAggregator eventAggregator)
    {
        this.smartScriptFactory = smartScriptFactory;
        this.eventAggregator = eventAggregator;
    }

    public override string Name => "smart_script_open";
    public override string Description => "Opens the SmartScript editor document for the given entry and script type in the editor UI, exactly as if the user opened it themselves. Use this to show the user a script; use smart_script_get to read one without touching the UI.";
    public override bool Mutating => true;

    protected override Task<string> Execute(SmartScriptRefInput input, CancellationToken token)
    {
        var type = SmartScriptMcpModel.ParseScriptType(input.ScriptType);
        var item = smartScriptFactory.Value.Factory(null, input.EntryOrGuid, type);
        eventAggregator.GetEvent<EventRequestOpenItem>().Publish(item);
        return Task.FromResult($"Opened SmartScript editor for {SmartScriptMcpModel.DescribeEntry(input.EntryOrGuid, type)}");
    }
}
