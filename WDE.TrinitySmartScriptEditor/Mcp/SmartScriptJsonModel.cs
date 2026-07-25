using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Services.MessageBox;
using WDE.Common.Services.Mcp;
using WDE.SmartScriptEditor;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Editor;
using WDE.SmartScriptEditor.Editor.ViewModels;
using WDE.SmartScriptEditor.Models;

namespace WDE.TrinitySmartScriptEditor.Mcp;

// ---------- output model (what smart_script_get returns; also accepted back by smart_script_update) ----------

public sealed class McpScript
{
    [Description("Entry (positive) or -guid (negative) the script belongs to")]
    public required int EntryOrGuid { get; init; }

    [Description("Script type name, i.e. Creature, GameObject, Quest, Spell, TimedActionList")]
    public required string ScriptType { get; init; }

    [Description("True when this is the live (possibly unsaved) state of a document currently open in the editor rather than the database state")]
    public bool FromOpenDocument { get; init; }

    public required List<McpEvent> Events { get; init; }
}

public sealed class McpParam
{
    [Description("0-based raw parameter slot; usable as 'index' on input")]
    public required int Index { get; init; }
    public required string Name { get; init; }
    public required long Value { get; init; }
    [Description("Human readable interpretation of the value when the editor knows one (spell name, flag names...)")]
    public string? Readable { get; init; }
}

public sealed class McpFloatParam
{
    [Description("0-based raw parameter slot; usable as 'index' on input")]
    public required int Index { get; init; }
    public required string Name { get; init; }
    public required double Value { get; init; }
}

public sealed class McpStringParam
{
    [Description("0-based raw parameter slot; usable as 'index' on input")]
    public required int Index { get; init; }
    public required string Name { get; init; }
    public required string Value { get; init; }
}

public sealed class McpEvent
{
    public required int Id { get; init; }
    [Description("Internal event name, i.e. SMART_EVENT_UPDATE_IC")]
    public required string Name { get; init; }
    [Description("Line number inside the script; inspection problems reference these lines")]
    public int Line { get; init; }
    [Description("Human readable one-line summary of the event")]
    public string? Readable { get; init; }
    public required long Chance { get; init; }
    public required long Flags { get; init; }
    public required long Phases { get; init; }
    public required long CooldownMin { get; init; }
    public required long CooldownMax { get; init; }
    public long TimerId { get; init; }
    [Description("Event parameters as the editor shows them (only slots the event defines, plus any undefined 'unusedN' slot holding a nonzero value)")]
    public required List<McpParam> Params { get; init; }
    public List<McpFloatParam>? FloatParams { get; init; }
    public List<McpStringParam>? StringParams { get; init; }
    public required List<McpAction> Actions { get; init; }
}

public sealed class McpAction
{
    public required int Id { get; init; }
    [Description("Internal action name, i.e. SMART_ACTION_TALK")]
    public required string Name { get; init; }
    [Description("Line number inside the script; inspection problems reference these lines")]
    public int Line { get; init; }
    [Description("Human readable one-line summary of the action")]
    public string? Readable { get; init; }
    [Description("Action parameters as the editor shows them (only slots the action defines, plus any undefined 'unusedN' slot holding a nonzero value)")]
    public required List<McpParam> Params { get; init; }
    public List<McpFloatParam>? FloatParams { get; init; }
    public List<McpStringParam>? StringParams { get; init; }
    public string? Comment { get; init; }
    public required McpSourceTarget Source { get; init; }
    public required McpSourceTarget Target { get; init; }
}

public sealed class McpSourceTarget
{
    public required int Id { get; init; }
    [Description("Internal source/target name, i.e. SMART_TARGET_SELF")]
    public required string Name { get; init; }
    public required List<McpParam> Params { get; init; }
    [Description("Present when the target defines custom float parameters (they share slots with x/y/z/o)")]
    public List<McpFloatParam>? FloatParams { get; init; }
    public double? X { get; init; }
    public double? Y { get; init; }
    public double? Z { get; init; }
    public double? O { get; init; }
    [Description("conditions table condition id attached to this target (0/absent = none)")]
    public long? ConditionId { get; init; }
}

public sealed class McpProblem
{
    [Description("Critical, Error, Warning or Info")]
    public required string Severity { get; init; }
    public required string Message { get; init; }
    [Description("Line number as reported by smart_script_get")]
    public required int Line { get; init; }
}

// ---------- input model (what smart_script_update / smart_script_validate accept) ----------

public sealed class McpScriptInput
{
    [Description("Events of the script, in order. The script in the database is fully replaced with this list.")]
    public required List<McpEventInput> Events { get; init; }
}

public sealed class McpEventInput
{
    [Description("Event id (see smart_data_search type=Event); either id or name is required")]
    public int? Id { get; init; }

    [Description("Event name, i.e. SMART_EVENT_UPDATE_IC; alternative to id")]
    public string? Name { get; init; }

    [Description("Percent chance to trigger (default 100)")]
    public long? Chance { get; init; }

    public long? Flags { get; init; }
    public long? Phases { get; init; }
    public long? CooldownMin { get; init; }
    public long? CooldownMax { get; init; }
    public long? TimerId { get; init; }

    [Description("Event parameter values: plain numbers (positional, slot order) or {value, index?, name?} objects as returned by smart_script_get ('index' = raw 0-based slot, 'name' = parameter or 'unusedN' name). Parameters not mentioned keep their defaults.")]
    public List<JsonElement>? Params { get; init; }

    public List<JsonElement>? FloatParams { get; init; }
    public List<JsonElement>? StringParams { get; init; }

    public List<McpActionInput>? Actions { get; init; }
}

public sealed class McpActionInput
{
    [Description("Action id (see smart_data_search type=Action); either id or name is required")]
    public int? Id { get; init; }

    [Description("Action name, i.e. SMART_ACTION_TALK; alternative to id")]
    public string? Name { get; init; }

    [Description("Action parameter values: plain numbers (positional, slot order) or {value, index?, name?} objects ('index' = raw 0-based slot, 'name' = parameter or 'unusedN' name). Parameters not mentioned keep their defaults.")]
    public List<JsonElement>? Params { get; init; }

    public List<JsonElement>? FloatParams { get; init; }
    public List<JsonElement>? StringParams { get; init; }

    public string? Comment { get; init; }

    [Description("Action source (who executes the action); defaults to none/implicit")]
    public McpSourceTargetInput? Source { get; init; }

    [Description("Action target; defaults to none")]
    public McpSourceTargetInput? Target { get; init; }
}

public sealed class McpSourceTargetInput
{
    [Description("Target/source id (see smart_data_search type=Target); either id or name is required")]
    public int? Id { get; init; }

    [Description("Target/source name, i.e. SMART_TARGET_SELF; alternative to id")]
    public string? Name { get; init; }

    public List<JsonElement>? Params { get; init; }

    [Description("Custom float parameters for targets that define them (share slots with x/y/z/o)")]
    public List<JsonElement>? FloatParams { get; init; }

    public double? X { get; init; }
    public double? Y { get; init; }
    public double? Z { get; init; }
    public double? O { get; init; }

    [Description("conditions table condition id attached to this target")]
    public long? ConditionId { get; init; }
}

// ---------- shared helpers ----------

internal sealed class McpEmptyMessageBoxService : IMessageBoxService
{
    public static readonly McpEmptyMessageBoxService Instance = new();
    public Task<T?> ShowDialog<T>(IMessageBox<T> messageBox) => Task.FromResult<T?>(default);
}

internal static class SmartScriptMcpModel
{
    private static readonly Regex MarkupRegex = new(@"\[/?[^\[\]]*\]", RegexOptions.Compiled);

    public static SmartScriptType ParseScriptType(string scriptType)
    {
        if (Enum.TryParse<SmartScriptType>(scriptType, true, out var type) &&
            type != SmartScriptType.END)
            return type;
        var valid = string.Join(", ", Enum.GetNames<SmartScriptType>().Where(n => n != nameof(SmartScriptType.END)));
        throw new McpToolException($"Unknown script type '{scriptType}'. Valid types: {valid}");
    }

    public static string DescribeEntry(int entryOrGuid, SmartScriptType type)
        => entryOrGuid < 0 ? $"guid {-entryOrGuid} ({type})" : $"entry {entryOrGuid} ({type})";

    /// <summary>Finds an open SmartScript editor document for the given entry/type, or null when it is not open.</summary>
    public static SmartScriptEditorViewModel? FindOpenDocument(IDocumentManager documentManager, int entryOrGuid, SmartScriptType type)
    {
        foreach (var document in documentManager.OpenedDocuments)
        {
            if (document is SmartScriptEditorViewModel viewModel &&
                viewModel.Script is { } openScript &&
                openScript.EntryOrGuid == entryOrGuid &&
                openScript.SourceType == type)
                return viewModel;
        }
        return null;
    }

    public static async Task<(SmartScriptSolutionItem item, SmartScript script)> LoadFromDatabase(
        int entryOrGuid,
        SmartScriptType type,
        ISmartScriptDatabaseProvider database,
        ISmartFactory smartFactory,
        ISmartDataManager smartDataManager,
        IEditorFeatures editorFeatures,
        ISmartScriptImporter importer,
        bool throwIfEmpty)
    {
        var item = new SmartScriptSolutionItem(entryOrGuid, type);
        var script = new SmartScript(item, smartFactory, smartDataManager, McpEmptyMessageBoxService.Instance, editorFeatures, importer);
        var lines = (await database.GetScriptFor(item.Entry ?? 0, item.EntryOrGuid, item.SmartType)).ToList();
        if (throwIfEmpty && lines.Count == 0)
            throw new McpToolException($"No SmartScript found for {DescribeEntry(entryOrGuid, type)}");
        var conditions = (await database.GetConditionsForScript(item.Entry, item.EntryOrGuid, item.SmartType)).ToList();
        await importer.Import(script, true, lines, conditions, null);
        return (item, script);
    }

    // ----- model -> json -----

    public static McpScript ToJsonModel(SmartScript script, ISmartDataManager smartDataManager, IEditorFeatures editorFeatures, bool fromOpenDocument = false)
    {
        var events = new List<McpEvent>();
        foreach (var ev in script.Events)
        {
            if (ev.IsGroup)
                continue;

            var eventData = TryGetData(smartDataManager, SmartType.SmartEvent, ev.Id);
            var actions = new List<McpAction>();
            foreach (var action in ev.Actions)
            {
                var actionData = TryGetData(smartDataManager, SmartType.SmartAction, action.Id);
                actions.Add(new McpAction
                {
                    Id = action.Id,
                    Name = actionData?.Name ?? $"unknown({action.Id})",
                    Line = action.VirtualLineId,
                    Readable = SafeReadable(action),
                    Params = IntParams(action, actionData),
                    FloatParams = FloatParams(action, actionData),
                    StringParams = StringParams(action, actionData),
                    Comment = string.IsNullOrEmpty(action.Comment) ? null : action.Comment,
                    Source = ToSourceTargetModel(action.Source, smartDataManager, editorFeatures.SourceHasPosition),
                    Target = ToSourceTargetModel(action.Target, smartDataManager, true)
                });
            }

            events.Add(new McpEvent
            {
                Id = ev.Id,
                Name = eventData?.Name ?? $"unknown({ev.Id})",
                Line = ev.VirtualLineId,
                Readable = SafeReadable(ev),
                Chance = ev.Chance.Value,
                Flags = ev.Flags.Value,
                Phases = ev.Phases.Value,
                CooldownMin = ev.CooldownMin.Value,
                CooldownMax = ev.CooldownMax.Value,
                TimerId = ev.TimerId.Value,
                Params = IntParams(ev, eventData),
                FloatParams = FloatParams(ev, eventData),
                StringParams = StringParams(ev, eventData),
                Actions = actions
            });
        }

        return new McpScript
        {
            EntryOrGuid = script.EntryOrGuid,
            ScriptType = script.SourceType.ToString(),
            FromOpenDocument = fromOpenDocument,
            Events = events
        };
    }

    private static McpSourceTarget ToSourceTargetModel(SmartSource sourceOrTarget, ISmartDataManager smartDataManager, bool hasPosition)
    {
        // the SmartTarget dictionary contains both targets and sources
        var data = TryGetData(smartDataManager, SmartType.SmartTarget, sourceOrTarget.Id);
        bool hasCustomFloats = data?.FloatParameters is { Count: > 0 };
        return new McpSourceTarget
        {
            Id = sourceOrTarget.Id,
            Name = data?.Name ?? $"unknown({sourceOrTarget.Id})",
            Params = IntParams(sourceOrTarget, data),
            FloatParams = hasCustomFloats ? FloatParams(sourceOrTarget, data) : null,
            X = hasPosition && !hasCustomFloats ? sourceOrTarget.X : null,
            Y = hasPosition && !hasCustomFloats ? sourceOrTarget.Y : null,
            Z = hasPosition && !hasCustomFloats ? sourceOrTarget.Z : null,
            O = hasPosition && !hasCustomFloats ? sourceOrTarget.O : null,
            ConditionId = sourceOrTarget.Condition.Value != 0 ? sourceOrTarget.Condition.Value : null
        };
    }

    private static SmartGenericJsonData? TryGetData(ISmartDataManager smartDataManager, SmartType type, int id)
        => smartDataManager.TryGetRawData(type, id, out var data) ? data : null;

    private static string? SafeReadable(SmartBaseElement element)
    {
        try
        {
            var readable = element.Readable;
            return string.IsNullOrEmpty(readable) ? null : MarkupRegex.Replace(readable, "");
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string UnusedName(int slot) => $"unused{slot + 1}";

    private static List<McpParam> IntParams(SmartBaseElement element, SmartGenericJsonData? data)
    {
        int definedCount = Math.Min(data?.Parameters?.Count ?? 0, element.ParametersCount);
        var list = new List<McpParam>(definedCount);
        for (int i = 0; i < element.ParametersCount; ++i)
        {
            var holder = element.GetParameter(i);
            bool isDefined = i < definedCount;
            if (!isDefined && holder.Value == 0)
                continue;

            string? readable = null;
            if (isDefined)
            {
                try
                {
                    var asString = holder.ToString();
                    if (asString != holder.Value.ToString())
                        readable = asString;
                }
                catch (Exception)
                {
                    // unresolvable readable value - raw value is enough
                }
            }
            list.Add(new McpParam
            {
                Index = i,
                Name = isDefined ? holder.Name : UnusedName(i),
                Value = holder.Value,
                Readable = readable
            });
        }
        return list;
    }

    private static List<McpFloatParam>? FloatParams(SmartBaseElement element, SmartGenericJsonData? data)
    {
        int definedCount = Math.Min(data?.FloatParameters?.Count ?? 0, element.FloatParametersCount);
        List<McpFloatParam>? list = null;
        for (int i = 0; i < element.FloatParametersCount; ++i)
        {
            var holder = element.GetFloatParameter(i);
            bool isDefined = i < definedCount;
            if (!isDefined && holder.Value == 0)
                continue;
            list ??= new List<McpFloatParam>();
            list.Add(new McpFloatParam { Index = i, Name = isDefined ? holder.Name : UnusedName(i), Value = holder.Value });
        }
        return list;
    }

    private static List<McpStringParam>? StringParams(SmartBaseElement element, SmartGenericJsonData? data)
    {
        int definedCount = Math.Min(data?.StringParameters?.Count ?? 0, element.StringParametersCount);
        List<McpStringParam>? list = null;
        for (int i = 0; i < element.StringParametersCount; ++i)
        {
            var holder = element.GetStringParameter(i);
            bool isDefined = i < definedCount;
            if (!isDefined && string.IsNullOrEmpty(holder.Value))
                continue;
            list ??= new List<McpStringParam>();
            list.Add(new McpStringParam { Index = i, Name = isDefined ? holder.Name : UnusedName(i), Value = holder.Value });
        }
        return list;
    }

    // ----- json -> model -----

    public static (SmartScriptSolutionItem item, SmartScript script) BuildFromJsonModel(
        int entryOrGuid,
        SmartScriptType type,
        McpScriptInput inputScript,
        ISmartFactory smartFactory,
        ISmartDataManager smartDataManager,
        IEditorFeatures editorFeatures,
        ISmartScriptImporter importer)
    {
        var item = new SmartScriptSolutionItem(entryOrGuid, type);
        var script = new SmartScript(item, smartFactory, smartDataManager, McpEmptyMessageBoxService.Instance, editorFeatures, importer);

        foreach (var ev in BuildEvents(script, inputScript, smartFactory, smartDataManager))
            script.Events.Add(ev);

        return (item, script);
    }

    /// <summary>Builds the events of the given JSON script without adding them to the script's Events collection
    /// (so a live, open script can be mutated atomically after all input has been validated).</summary>
    public static List<SmartEvent> BuildEvents(
        SmartScript script,
        McpScriptInput inputScript,
        ISmartFactory smartFactory,
        ISmartDataManager smartDataManager)
    {
        var events = new List<SmartEvent>();
        int eventIndex = 0;
        foreach (var eventInput in inputScript.Events)
        {
            eventIndex++;
            events.Add(BuildEvent(script, eventInput, eventIndex, smartFactory, smartDataManager));
        }
        return events;
    }

    private static SmartEvent BuildEvent(SmartScript script, McpEventInput input, int eventIndex, ISmartFactory smartFactory, ISmartDataManager smartDataManager)
    {
        var context = $"event #{eventIndex}";
        int id = ResolveId(input.Id, input.Name, SmartType.SmartEvent, smartDataManager, context);

        SmartEvent ev;
        try
        {
            ev = smartFactory.EventFactory(script, id);
        }
        catch (Exception e)
        {
            throw new McpToolException($"Cannot create {context} (id {id}): {e.Message}");
        }

        if (input.Chance.HasValue)
            ev.Chance.Value = input.Chance.Value;
        if (input.Flags.HasValue)
            ev.Flags.Value = input.Flags.Value;
        if (input.Phases.HasValue)
            ev.Phases.Value = input.Phases.Value;
        if (input.CooldownMin.HasValue)
            ev.CooldownMin.Value = input.CooldownMin.Value;
        if (input.CooldownMax.HasValue)
            ev.CooldownMax.Value = input.CooldownMax.Value;
        if (input.TimerId.HasValue)
            ev.TimerId.Value = input.TimerId.Value;

        ApplyParams(ev, input.Params, input.FloatParams, input.StringParams, context);

        if (input.Actions != null)
        {
            int actionIndex = 0;
            foreach (var actionInput in input.Actions)
            {
                actionIndex++;
                ev.AddAction(BuildAction(actionInput, $"{context} action #{actionIndex}", smartFactory, smartDataManager));
            }
        }

        return ev;
    }

    private static SmartAction BuildAction(McpActionInput input, string context, ISmartFactory smartFactory, ISmartDataManager smartDataManager)
    {
        int actionId = ResolveId(input.Id, input.Name, SmartType.SmartAction, smartDataManager, context);

        SmartSource source = BuildSource(input.Source, $"{context} source", smartFactory, smartDataManager);
        SmartTarget target = BuildTarget(input.Target, $"{context} target", smartFactory, smartDataManager);

        // apply the implicit source the same way the database importer does
        if (input.Source == null &&
            smartDataManager.TryGetRawData(SmartType.SmartAction, actionId, out var actionData) &&
            actionData.ImplicitSource != null &&
            smartDataManager.Contains(SmartType.SmartSource, actionData.ImplicitSource))
        {
            smartFactory.SafeUpdateSource(source, smartDataManager.GetDataByName(SmartType.SmartSource, actionData.ImplicitSource).Id);
        }

        SmartAction action;
        try
        {
            action = smartFactory.ActionFactory(actionId, source, target);
        }
        catch (Exception e)
        {
            throw new McpToolException($"Cannot create {context} (id {actionId}): {e.Message}");
        }

        ApplyParams(action, input.Params, input.FloatParams, input.StringParams, context);

        if (input.Comment != null)
            action.Comment = input.Comment;

        return action;
    }

    private static SmartSource BuildSource(McpSourceTargetInput? input, string context, ISmartFactory smartFactory, ISmartDataManager smartDataManager)
    {
        int id = input == null ? 0 : ResolveSourceTargetId(input.Id, input.Name, smartDataManager, context);
        SmartSource source;
        try
        {
            source = smartFactory.SourceFactory(id);
        }
        catch (Exception e)
        {
            throw new McpToolException($"Cannot create {context} (id {id}): {e.Message}");
        }
        if (input != null)
            ApplySourceTarget(source, input, context);
        return source;
    }

    private static SmartTarget BuildTarget(McpSourceTargetInput? input, string context, ISmartFactory smartFactory, ISmartDataManager smartDataManager)
    {
        int id = input == null ? 0 : ResolveSourceTargetId(input.Id, input.Name, smartDataManager, context);
        SmartTarget target;
        try
        {
            target = smartFactory.TargetFactory(id);
        }
        catch (Exception e)
        {
            throw new McpToolException($"Cannot create {context} (id {id}): {e.Message}");
        }
        if (input != null)
            ApplySourceTarget(target, input, context);
        return target;
    }

    private static void ApplySourceTarget(SmartSource sourceOrTarget, McpSourceTargetInput input, string context)
    {
        ApplyParams(sourceOrTarget, input.Params, input.FloatParams, null, context);
        if (input.X.HasValue)
            sourceOrTarget.X = (float)input.X.Value;
        if (input.Y.HasValue)
            sourceOrTarget.Y = (float)input.Y.Value;
        if (input.Z.HasValue)
            sourceOrTarget.Z = (float)input.Z.Value;
        if (input.O.HasValue)
            sourceOrTarget.O = (float)input.O.Value;
        if (input.ConditionId.HasValue)
            sourceOrTarget.Condition.Value = input.ConditionId.Value;
    }

    private static void ApplyParams(SmartBaseElement element, List<JsonElement>? intParams, List<JsonElement>? floatParams, List<JsonElement>? stringParams, string context)
    {
        if (intParams != null)
        {
            for (int i = 0; i < intParams.Count; ++i)
            {
                int slot = ResolveSlot(intParams[i], i, element.ParametersCount,
                    s => element.GetParameter(s).Name, $"{context} parameter {i + 1}");
                element.GetParameter(slot).Value = ReadLong(intParams[i], $"{context} parameter {i + 1}");
            }
        }

        if (floatParams != null)
        {
            for (int i = 0; i < floatParams.Count; ++i)
            {
                int slot = ResolveSlot(floatParams[i], i, element.FloatParametersCount,
                    s => element.GetFloatParameter(s).Name, $"{context} float parameter {i + 1}");
                element.GetFloatParameter(slot).Value = (float)ReadDouble(floatParams[i], $"{context} float parameter {i + 1}");
            }
        }

        if (stringParams != null)
        {
            for (int i = 0; i < stringParams.Count; ++i)
            {
                int slot = ResolveSlot(stringParams[i], i, element.StringParametersCount,
                    s => element.GetStringParameter(s).Name, $"{context} string parameter {i + 1}");
                element.GetStringParameter(slot).Value = ReadString(stringParams[i], $"{context} string parameter {i + 1}");
            }
        }
    }

    /// <summary>Resolves which raw parameter slot an input array element addresses:
    /// an object with "index" wins, then an object with "name" (parameter name or "unusedN"),
    /// otherwise the element's position in the array.</summary>
    private static int ResolveSlot(JsonElement element, int position, int slotCount, Func<int, string> slotName, string context)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if ((element.TryGetProperty("index", out var indexProperty) || element.TryGetProperty("Index", out indexProperty)) &&
                indexProperty.ValueKind == JsonValueKind.Number)
            {
                int slot = indexProperty.GetInt32();
                if (slot < 0 || slot >= slotCount)
                    throw new McpToolException($"{context}: index {slot} out of range (0..{slotCount - 1})");
                return slot;
            }

            if ((element.TryGetProperty("name", out var nameProperty) || element.TryGetProperty("Name", out nameProperty)) &&
                nameProperty.ValueKind == JsonValueKind.String)
            {
                var name = nameProperty.GetString() ?? "";
                if (TryParseUnusedName(name, out var unusedSlot))
                {
                    if (unusedSlot >= slotCount)
                        throw new McpToolException($"{context}: '{name}' out of range, there are only {slotCount} slots");
                    return unusedSlot;
                }

                for (int slot = 0; slot < slotCount; ++slot)
                {
                    if (string.Equals(slotName(slot), name, StringComparison.OrdinalIgnoreCase))
                        return slot;
                }
                throw new McpToolException($"{context}: unknown parameter name '{name}'");
            }
        }

        if (position >= slotCount)
            throw new McpToolException($"{context}: too many positional parameters, at most {slotCount} accepted");
        return position;
    }

    private static bool TryParseUnusedName(string name, out int slot)
    {
        slot = 0;
        if (!name.StartsWith("unused", StringComparison.OrdinalIgnoreCase))
            return false;
        if (!int.TryParse(name.AsSpan(6), out var oneBased) || oneBased < 1)
            return false;
        slot = oneBased - 1;
        return true;
    }

    private static int ResolveId(int? id, string? name, SmartType type, ISmartDataManager smartDataManager, string context)
    {
        var kind = type == SmartType.SmartEvent ? "event" : type == SmartType.SmartAction ? "action" : "target";
        if (id.HasValue)
        {
            if (!smartDataManager.Contains(type, id.Value))
                throw new McpToolException($"{context}: unknown {kind} id {id.Value} (use smart_data_search)");
            return id.Value;
        }

        if (!string.IsNullOrEmpty(name))
        {
            if (!smartDataManager.Contains(type, name))
                throw new McpToolException($"{context}: unknown {kind} name '{name}' (use smart_data_search)");
            return smartDataManager.GetDataByName(type, name).Id;
        }

        throw new McpToolException($"{context}: either 'id' or 'name' is required");
    }

    private static int ResolveSourceTargetId(int? id, string? name, ISmartDataManager smartDataManager, string context)
    {
        if (id.HasValue)
        {
            // the SmartTarget dictionary contains both targets and sources
            if (!smartDataManager.Contains(SmartType.SmartTarget, id.Value) &&
                !smartDataManager.Contains(SmartType.SmartSource, id.Value))
                throw new McpToolException($"{context}: unknown source/target id {id.Value} (use smart_data_search)");
            return id.Value;
        }

        if (!string.IsNullOrEmpty(name))
        {
            if (smartDataManager.Contains(SmartType.SmartTarget, name))
                return smartDataManager.GetDataByName(SmartType.SmartTarget, name).Id;
            if (smartDataManager.Contains(SmartType.SmartSource, name))
                return smartDataManager.GetDataByName(SmartType.SmartSource, name).Id;
            throw new McpToolException($"{context}: unknown source/target name '{name}' (use smart_data_search)");
        }

        throw new McpToolException($"{context}: either 'id' or 'name' is required");
    }

    private static long ReadLong(JsonElement element, string context)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Number:
                return element.TryGetInt64(out var asLong) ? asLong : (long)element.GetDouble();
            case JsonValueKind.String when long.TryParse(element.GetString(), out var parsed):
                return parsed;
            case JsonValueKind.True:
                return 1;
            case JsonValueKind.False:
                return 0;
            case JsonValueKind.Object when TryGetValueProperty(element, out var inner):
                return ReadLong(inner, context);
            default:
                throw new McpToolException($"{context}: expected a number or {{\"value\": number}}, got {element.ValueKind}");
        }
    }

    private static double ReadDouble(JsonElement element, string context)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Number:
                return element.GetDouble();
            case JsonValueKind.String when double.TryParse(element.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed):
                return parsed;
            case JsonValueKind.Object when TryGetValueProperty(element, out var inner):
                return ReadDouble(inner, context);
            default:
                throw new McpToolException($"{context}: expected a number or {{\"value\": number}}, got {element.ValueKind}");
        }
    }

    private static string ReadString(JsonElement element, string context)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return element.GetString() ?? "";
            case JsonValueKind.Number:
                return element.ToString();
            case JsonValueKind.Object when TryGetValueProperty(element, out var inner):
                return ReadString(inner, context);
            default:
                throw new McpToolException($"{context}: expected a string or {{\"value\": string}}, got {element.ValueKind}");
        }
    }

    private static bool TryGetValueProperty(JsonElement element, out JsonElement value)
        => element.TryGetProperty("value", out value) || element.TryGetProperty("Value", out value);
}
