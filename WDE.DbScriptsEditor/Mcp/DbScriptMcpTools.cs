using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Prism.Events;
using WDE.Common.Database;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Services.Mcp;
using WDE.DbScriptsEditor.Data;
using WDE.DbScriptsEditor.Editor;
using WDE.DbScriptsEditor.Editor.ViewModels;
using WDE.DbScriptsEditor.Exporter;
using WDE.DbScriptsEditor.Models;
using WDE.Module.Attributes;

namespace WDE.DbScriptsEditor.Mcp;

// The MCP surface deliberately mirrors the EDITOR's representation, not the raw table columns:
// named parameters, structural source/target (+ buddy locator), wait/comment/if rows. The raw
// datalong/data_flags encoding stays an implementation detail, exactly like in the UI.

public sealed class DbScriptCommandsInput
{
    [Description("Optional case-insensitive substring filter over command names")]
    public string? Filter { get; init; }

    [Description("Optional exact command id")]
    public uint? Id { get; init; }
}

public sealed class DbScriptParameterInfo
{
    public required string Name { get; init; }
    [Description("Value type: a parameter key usable with parameter_search, or a plain type")]
    public required string Type { get; init; }
    public bool Required { get; init; }
    public long Default { get; init; }
}

public sealed class DbScriptVariantInfo
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public List<DbScriptParameterInfo>? Parameters { get; init; }
}

public sealed class DbScriptCommandInfo
{
    public required uint Id { get; init; }
    public required string Name { get; init; }
    public required string ReadableName { get; init; }
    public string? Help { get; init; }
    public string? Group { get; init; }
    public bool Deprecated { get; init; }
    public List<DbScriptVariantInfo>? Variants { get; init; }
}

[AutoRegister]
[SingleInstance]
public class DbScriptCommandsTool : McpTool<DbScriptCommandsInput, List<DbScriptCommandInfo>>
{
    private readonly Lazy<IDbScriptDataManager> dataManager;

    public DbScriptCommandsTool(Lazy<IDbScriptDataManager> dataManager)
    {
        this.dataManager = dataManager;
    }

    public override string Name => "dbscript_commands";
    public override string Description => "Lists the cmangos dbscripts commands (TALK, MOVE_TO, CAST_SPELL, ...) with their named parameters, variants and help. Use the parameter names with dbscript_update.";

    protected override Task<List<DbScriptCommandInfo>> Execute(DbScriptCommandsInput input, CancellationToken token)
    {
        var commands = dataManager.Value.AllCommands.AsEnumerable();
        if (input.Id.HasValue)
            commands = commands.Where(c => c.Id == input.Id.Value);
        if (!string.IsNullOrEmpty(input.Filter))
            commands = commands.Where(c => c.Name.Contains(input.Filter, StringComparison.OrdinalIgnoreCase) ||
                                           c.NameReadable.Contains(input.Filter, StringComparison.OrdinalIgnoreCase));

        var result = commands.Select(c => new DbScriptCommandInfo
        {
            Id = c.Id,
            Name = c.Name,
            ReadableName = c.NameReadable,
            Help = c.Help,
            Group = c.Group,
            Deprecated = c.Deprecated,
            Variants = c.Variants?.Select(v => new DbScriptVariantInfo
            {
                Name = v.NameReadable,
                Description = v.Description,
                Parameters = v.Parameters?.Select(p => new DbScriptParameterInfo
                {
                    Name = p.Name,
                    Type = p.Type,
                    Required = p.Required,
                    Default = p.DefaultVal
                }).ToList()
            }).ToList()
        }).ToList();
        return Task.FromResult(result);
    }
}

public sealed class DbScriptListIdsInput
{
    [Description("Script table (CreatureDeath, CreatureMovement, Event, GoUse, GoTemplateUse, Gossip, QuestStart, QuestEnd, Spell, Relay)")]
    public required DbScriptType Type { get; init; }
}

[AutoRegister]
[SingleInstance]
public class DbScriptListIdsTool : McpTool<DbScriptListIdsInput, IReadOnlyList<uint>>
{
    private readonly Lazy<IDbScriptDatabaseProvider> database;

    public DbScriptListIdsTool(Lazy<IDbScriptDatabaseProvider> database)
    {
        this.database = database;
    }

    public override string Name => "dbscript_list_ids";
    public override string Description => "Lists all script ids present in a given dbscripts_on_* table.";

    protected override Task<IReadOnlyList<uint>> Execute(DbScriptListIdsInput input, CancellationToken token)
        => database.Value.GetScriptIds(input.Type);
}

public enum DbScriptRowKind
{
    Step,
    Wait,
    Comment,
    If
}

public sealed class DbScriptParamJson
{
    [Description("Parameter name as shown in the editor (see dbscript_commands / dbscript_get)")]
    public required string Name { get; init; }
    public required double Value { get; init; }
    [Description("Readable value (spell/creature names etc.); output only, ignored on input")]
    public string? Readable { get; init; }
}

public sealed class DbScriptBuddyJson
{
    [Description("How the buddy object is located: NearestByEntry, ByGuid (searchValue = guid), ByPool (searchValue = pool id), BySpawnGroup, ByStringId, Pet")]
    public required BuddyFindMode Mode { get; init; }

    [Description("True when the buddy is a gameobject instead of a creature")]
    public bool IsGameObject { get; init; }

    [Description("Buddy creature/gameobject entry (meaning depends on mode)")]
    public long Entry { get; init; }

    [Description("Search radius in yards, or the guid / pool id depending on mode")]
    public long SearchValue { get; init; }

    public bool IncludeDespawned { get; init; }
    public bool AllEligible { get; init; }

    [Description("Resolved buddy name; output only")]
    public string? Readable { get; init; }
}

public sealed class DbScriptRowJson
{
    [Description("Row kind, exactly as in the editor: Step (a command), Wait (time gap), Comment, If (condition block opener)")]
    public required DbScriptRowKind Kind { get; init; }

    [Description("True when this row belongs to the condition block opened by the nearest preceding If row (unbroken run)")]
    public bool InIf { get; init; }

    [Description("Wait rows: duration in milliseconds")]
    public long? Milliseconds { get; init; }

    [Description("Comment rows: the text")]
    public string? Text { get; init; }

    [Description("If rows: the mangos_conditions condition id guarding the block")]
    public long? ConditionId { get; init; }

    [Description("Step rows: command id (see dbscript_commands); either this or commandName")]
    public uint? Command { get; init; }

    [Description("Step rows: command name, i.e. 'TALK' (alternative to command id on input)")]
    public string? CommandName { get; init; }

    [Description("Resolved command variant; output only")]
    public string? Variant { get; init; }

    [Description("The editor's readable one-line summary of the step; output only")]
    public string? Readable { get; init; }

    [Description("Step rows: named parameters (only the ones the command uses, like in the editor)")]
    public List<DbScriptParamJson>? Params { get; init; }

    [Description("Step rows: who the step acts as: OriginalSource (the script's built-in source), OriginalTarget or Buddy. Default OriginalSource")]
    public SourceTargetKind? Source { get; init; }

    [Description("Step rows: whom the step acts on: OriginalSource, OriginalTarget or Buddy. Default OriginalTarget")]
    public SourceTargetKind? Target { get; init; }

    [Description("Step rows: buddy locator; required when source or target is Buddy")]
    public DbScriptBuddyJson? Buddy { get; init; }

    [Description("Step rows: exotic unmodeled data_flags bits passthrough; normally omit")]
    public uint? UnmodeledFlagBits { get; init; }
}

public sealed class DbScriptGetInput
{
    [Description("Script table (CreatureDeath, CreatureMovement, Event, GoUse, GoTemplateUse, Gossip, QuestStart, QuestEnd, Spell, Relay)")]
    public required DbScriptType Type { get; init; }

    [Description("Script id (i.e. creature entry for CreatureDeath, relay id for Relay)")]
    public required uint Id { get; init; }
}

public sealed class DbScriptGetOutput
{
    public required string Table { get; init; }
    public required uint ScriptId { get; init; }
    [Description("What 'OriginalSource' means for this script type, i.e. 'Dying creature'")]
    public required string SourceLabel { get; init; }
    [Description("What 'OriginalTarget' means for this script type, i.e. 'Killer'")]
    public required string TargetLabel { get; init; }
    [Description("True when this reflects the (possibly unsaved) state of a document currently open in the editor")]
    public required bool FromOpenDocument { get; init; }
    [Description("The script exactly as the editor shows it; this is also the shape dbscript_update accepts")]
    public required List<DbScriptRowJson> Rows { get; init; }
}

public sealed class DbScriptProblem
{
    public required int StepIndex { get; init; }
    public required string Step { get; init; }
    public required string Severity { get; init; }
    public required string Message { get; init; }
}

public abstract class DbScriptModelToolBase<TInput, TOutput> : McpTool<TInput, TOutput> where TInput : class
{
    protected readonly Lazy<IDbScriptDatabaseProvider> Database;
    protected readonly Lazy<IDbScriptDataManager> DataManager;
    protected readonly Lazy<IParameterFactory> ParameterFactory;
    protected readonly Lazy<IDocumentManager> DocumentManager;

    protected DbScriptModelToolBase(Lazy<IDbScriptDatabaseProvider> database,
        Lazy<IDbScriptDataManager> dataManager,
        Lazy<IParameterFactory> parameterFactory,
        Lazy<IDocumentManager> documentManager)
    {
        Database = database;
        DataManager = dataManager;
        ParameterFactory = parameterFactory;
        DocumentManager = documentManager;
    }

    protected (EditableDbScript script, DbScriptEditorViewModel document)? TryGetOpenDocument(DbScriptType type, uint id)
    {
        foreach (var document in DocumentManager.Value.OpenedDocuments)
        {
            if (document is DbScriptEditorViewModel vm &&
                vm.SolutionItem is DbScriptSolutionItem item &&
                item.ScriptType == type && item.ScriptId == id &&
                vm.Script != null)
                return (vm.Script, vm);
        }
        return null;
    }

    protected async Task<(EditableDbScript script, bool fromOpenDocument)> LoadScript(DbScriptType type, uint id)
    {
        if (TryGetOpenDocument(type, id) is { } open)
            return (open.script, true);

        var lines = await Database.Value.GetScript(type, id);
        var script = new EditableDbScript(type, id, DataManager.Value, ParameterFactory.Value);
        script.Load(lines);
        return (script, false);
    }

    protected static List<DbScriptRowJson> RowsToJson(EditableDbScript script)
    {
        var rows = new List<DbScriptRowJson>();
        foreach (var row in script.Rows)
        {
            switch (row)
            {
                case EditableDbScriptStep step:
                    rows.Add(StepToJson(step));
                    break;
                case DbScriptWaitRow wait:
                    rows.Add(new DbScriptRowJson { Kind = DbScriptRowKind.Wait, InIf = wait.InIf, Milliseconds = wait.Duration.Value });
                    break;
                case DbScriptCommentRow comment:
                    rows.Add(new DbScriptRowJson { Kind = DbScriptRowKind.Comment, InIf = comment.InIf, Text = comment.Text.Value });
                    break;
                case DbScriptIfRow ifRow:
                    rows.Add(new DbScriptRowJson { Kind = DbScriptRowKind.If, ConditionId = ifRow.ConditionId.Value });
                    break;
            }
        }
        return rows;
    }

    private static DbScriptRowJson StepToJson(EditableDbScriptStep step)
    {
        var decoded = step.DecodeFlags();

        var parameters = new List<DbScriptParamJson>();
        foreach (var p in step.UsedParameters)
        {
            var value = p.IsFloat ? p.FloatHolder!.Value : (double)p.LongHolder!.Value;
            var readable = p.StringValue;
            parameters.Add(new DbScriptParamJson
            {
                Name = p.Name,
                Value = value,
                Readable = readable != value.ToString(System.Globalization.CultureInfo.InvariantCulture) ? readable : null
            });
        }
        // The per-command 0x8 switch is shown as a regular named parameter, like in the edit dialog.
        if (step.AdditionalFlagHolder.IsUsed)
        {
            parameters.Add(new DbScriptParamJson
            {
                Name = step.AdditionalFlagHolder.Name,
                Value = step.AdditionalFlagHolder.Value,
                Readable = step.AdditionalFlagHolder.String
            });
        }

        DbScriptBuddyJson? buddy = null;
        if (decoded.Buddy.Provided)
        {
            buddy = new DbScriptBuddyJson
            {
                Mode = decoded.Buddy.Mode,
                IsGameObject = decoded.Buddy.IsGameObject,
                Entry = decoded.Buddy.Entry,
                SearchValue = decoded.Buddy.SearchValue,
                IncludeDespawned = decoded.Buddy.IncludeDespawned,
                AllEligible = decoded.Buddy.AllEligible,
                Readable = step.BuddyEntry.String
            };
        }

        return new DbScriptRowJson
        {
            Kind = DbScriptRowKind.Step,
            InIf = step.InIf,
            Command = step.CommandId,
            CommandName = step.CommandName,
            Variant = step.VariantName,
            Readable = step.Readable,
            Params = parameters,
            Source = decoded.Direction.Source,
            Target = decoded.Direction.Target,
            Buddy = buddy,
            UnmodeledFlagBits = decoded.UnmodeledBits != 0 ? decoded.UnmodeledBits : null
        };
    }

    protected void BuildRows(EditableDbScript script, List<DbScriptRowJson> rows)
    {
        foreach (var row in rows)
        {
            DbScriptRow built = row.Kind switch
            {
                DbScriptRowKind.Wait => script.MakeWait(row.Milliseconds ?? throw new McpToolException("Wait row requires milliseconds")),
                DbScriptRowKind.Comment => script.MakeComment(row.Text ?? throw new McpToolException("Comment row requires text")),
                DbScriptRowKind.If => script.MakeIf(row.ConditionId ?? throw new McpToolException("If row requires conditionId")),
                DbScriptRowKind.Step => BuildStep(script, row),
                _ => throw new McpToolException($"Unknown row kind {row.Kind}")
            };
            built.InIf = row.Kind != DbScriptRowKind.If && row.InIf;
            script.Rows.Add(built);
        }
        script.NormalizeIfMembership();
        script.SyncDerived();
    }

    private EditableDbScriptStep BuildStep(EditableDbScript script, DbScriptRowJson row)
    {
        uint commandId;
        if (row.Command.HasValue)
            commandId = row.Command.Value;
        else if (!string.IsNullOrEmpty(row.CommandName))
        {
            var definition = DataManager.Value.AllCommands.FirstOrDefault(c =>
                string.Equals(c.Name, row.CommandName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.NameReadable, row.CommandName, StringComparison.OrdinalIgnoreCase));
            commandId = definition?.Id ?? throw new McpToolException($"Unknown command name '{row.CommandName}' (see dbscript_commands)");
        }
        else
            throw new McpToolException("Step row requires command or commandName");

        if (DataManager.Value.TryGetCommand(commandId) == null)
            throw new McpToolException($"Unknown command id {commandId} (see dbscript_commands)");

        var step = script.MakeStep(new AbstractDbScriptLine { Id = script.ScriptId, Command = commandId });

        // structural source/target/buddy first: it owns data_flags wholesale
        var source = row.Source ?? SourceTargetKind.OriginalSource;
        var target = row.Target ?? SourceTargetKind.OriginalTarget;
        var direction = new ScriptDirection(source, target);
        var buddy = BuddyDescriptor.None;
        if (row.Buddy is { } buddyJson)
        {
            buddy = new BuddyDescriptor(buddyJson.Mode, buddyJson.IsGameObject, buddyJson.Entry,
                buddyJson.SearchValue, buddyJson.IncludeDespawned, buddyJson.AllEligible);
        }
        if (direction.UsesBuddy && !buddy.Provided)
            throw new McpToolException("Source/target uses Buddy but no buddy locator was given");
        var decoded = new DecodedFlags(direction, false, buddy, row.UnmodeledFlagBits ?? 0);
        if (!DbScriptFlagsCodec.TryEncode(decoded, out _, out _, out _))
            throw new McpToolException($"Source/target combination {source} -> {target} is not representable");
        step.ApplyDecodedFlags(decoded);

        // then named parameters, exactly the names the editor shows (incl. the 0x8 switch)
        foreach (var parameter in row.Params ?? new List<DbScriptParamJson>())
        {
            var used = step.UsedParameters.FirstOrDefault(p =>
                string.Equals(p.Name, parameter.Name, StringComparison.OrdinalIgnoreCase));
            if (used != null)
            {
                if (used.IsFloat)
                    used.FloatHolder!.Value = (float)parameter.Value;
                else
                    used.LongHolder!.Value = (long)Math.Round(parameter.Value);
                continue;
            }

            if (step.AdditionalFlagHolder.IsUsed &&
                string.Equals(step.AdditionalFlagHolder.Name, parameter.Name, StringComparison.OrdinalIgnoreCase))
            {
                step.AdditionalFlagHolder.Value = (long)Math.Round(parameter.Value);
                continue;
            }

            var known = step.UsedParameters.Select(p => p.Name)
                .Concat(step.AdditionalFlagHolder.IsUsed ? new[] { step.AdditionalFlagHolder.Name } : Array.Empty<string>());
            throw new McpToolException($"Command {step.CommandName} has no parameter '{parameter.Name}'. Known parameters: {string.Join(", ", known)}");
        }

        return step;
    }

    protected static List<DbScriptProblem> InspectScript(EditableDbScript script)
    {
        var steps = script.Steps;
        var perStep = DbScriptInspections.Inspect(steps, script.TypeInfo);
        var problems = new List<DbScriptProblem>();
        for (int i = 0; i < steps.Count && i < perStep.Count; ++i)
        {
            foreach (var diagnostic in perStep[i])
            {
                problems.Add(new DbScriptProblem
                {
                    StepIndex = i,
                    Step = steps[i].Readable,
                    Severity = diagnostic.Severity.ToString(),
                    Message = diagnostic.Message
                });
            }
        }
        return problems;
    }
}

[AutoRegister]
[SingleInstance]
public class DbScriptGetTool : DbScriptModelToolBase<DbScriptGetInput, DbScriptGetOutput>
{
    public DbScriptGetTool(Lazy<IDbScriptDatabaseProvider> database,
        Lazy<IDbScriptDataManager> dataManager,
        Lazy<IParameterFactory> parameterFactory,
        Lazy<IDocumentManager> documentManager)
        : base(database, dataManager, parameterFactory, documentManager) { }

    public override string Name => "dbscript_get";
    public override string Description => "Reads a cmangos dbscript the way the editor presents it: a timeline of steps (named parameters, structural source/target/buddy), waits, comments and if-blocks. If the script's document is open in the editor, returns its live (possibly unsaved) state.";

    protected override async Task<DbScriptGetOutput> Execute(DbScriptGetInput input, CancellationToken token)
    {
        var (script, fromOpenDocument) = await LoadScript(input.Type, input.Id);
        if (!fromOpenDocument && script.Rows.Count == 0)
            throw new McpToolException($"No script {input.Id} in {DbScriptTypes.TableName(input.Type)}");
        var info = DbScriptTypes.GetInfo(input.Type);
        return new DbScriptGetOutput
        {
            Table = info.TableName,
            ScriptId = input.Id,
            SourceLabel = info.SourceLabel,
            TargetLabel = info.TargetLabel,
            FromOpenDocument = fromOpenDocument,
            Rows = RowsToJson(script)
        };
    }
}

public sealed class DbScriptValidateInput
{
    [Description("Script table")]
    public required DbScriptType Type { get; init; }

    [Description("Script id")]
    public required uint Id { get; init; }

    [Description("Optional rows to validate instead of the current script (same shape as dbscript_get returns)")]
    public List<DbScriptRowJson>? Rows { get; init; }
}

[AutoRegister]
[SingleInstance]
public class DbScriptValidateTool : DbScriptModelToolBase<DbScriptValidateInput, List<DbScriptProblem>>
{
    public DbScriptValidateTool(Lazy<IDbScriptDatabaseProvider> database,
        Lazy<IDbScriptDataManager> dataManager,
        Lazy<IParameterFactory> parameterFactory,
        Lazy<IDocumentManager> documentManager)
        : base(database, dataManager, parameterFactory, documentManager) { }

    public override string Name => "dbscript_validate";
    public override string Description => "Runs the dbscripts editor's inspections over a script (the open document / database state, or over provided rows) and returns per-step diagnostics.";

    protected override async Task<List<DbScriptProblem>> Execute(DbScriptValidateInput input, CancellationToken token)
    {
        EditableDbScript script;
        if (input.Rows != null)
        {
            script = new EditableDbScript(input.Type, input.Id, DataManager.Value, ParameterFactory.Value);
            BuildRows(script, input.Rows);
        }
        else
            (script, _) = await LoadScript(input.Type, input.Id);
        return InspectScript(script);
    }
}

public sealed class DbScriptUpdateInput
{
    [Description("Script table")]
    public required DbScriptType Type { get; init; }

    [Description("Script id")]
    public required uint Id { get; init; }

    [Description("The full new content of the script (replaces all rows; same shape as dbscript_get returns)")]
    public required List<DbScriptRowJson> Rows { get; init; }

    [Description("When true AND the document is not open, the generated SQL is executed against the world database. Default false = only return SQL for review")]
    public bool Execute { get; init; }
}

public sealed class DbScriptUpdateOutput
{
    [Description("True when the change was applied to the open editor document (visible, undoable; save it with document_save)")]
    public required bool AppliedToOpenDocument { get; init; }
    [Description("The SQL this script would save as")]
    public required string Sql { get; init; }
    public required bool Executed { get; init; }
    public required List<DbScriptProblem> Problems { get; init; }
}

[AutoRegister]
[SingleInstance]
public class DbScriptUpdateTool : DbScriptModelToolBase<DbScriptUpdateInput, DbScriptUpdateOutput>
{
    private readonly Lazy<IDbScriptExporter> exporter;
    private readonly Lazy<IMySqlExecutor> sqlExecutor;

    public DbScriptUpdateTool(Lazy<IDbScriptDatabaseProvider> database,
        Lazy<IDbScriptDataManager> dataManager,
        Lazy<IParameterFactory> parameterFactory,
        Lazy<IDocumentManager> documentManager,
        Lazy<IDbScriptExporter> exporter,
        Lazy<IMySqlExecutor> sqlExecutor)
        : base(database, dataManager, parameterFactory, documentManager)
    {
        this.exporter = exporter;
        this.sqlExecutor = sqlExecutor;
    }

    public override string Name => "dbscript_update";
    public override string Description => "Replaces a cmangos dbscript with the given rows (editor representation: steps with named params + source/target, waits, comments, if-blocks). If the script's document is OPEN in the editor, the change is applied to the document (visible + undoable; user saves via document_save). Otherwise generates the same SQL the editor's Save produces; pass execute=true to apply it to the database.";
    public override bool Mutating => true;

    protected override async Task<DbScriptUpdateOutput> Execute(DbScriptUpdateInput input, CancellationToken token)
    {
        if (TryGetOpenDocument(input.Type, input.Id) is { } open)
        {
            if (input.Execute)
                throw new McpToolException("This script's document is open in the editor - changes were NOT applied. Call again with execute=false to edit the open document, then save it with document_save.");

            using (open.script.BulkEdit("MCP edit"))
            {
                while (open.script.Rows.Count > 0)
                    open.script.Rows.RemoveAt(open.script.Rows.Count - 1);
                BuildRows(open.script, input.Rows);
            }

            return new DbScriptUpdateOutput
            {
                AppliedToOpenDocument = true,
                Sql = exporter.Value.GenerateSql(open.script).QueryString,
                Executed = false,
                Problems = InspectScript(open.script)
            };
        }

        var script = new EditableDbScript(input.Type, input.Id, DataManager.Value, ParameterFactory.Value);
        BuildRows(script, input.Rows);
        var problems = InspectScript(script);
        var query = exporter.Value.GenerateSql(script);
        if (input.Execute)
            await sqlExecutor.Value.ExecuteSql(query);
        return new DbScriptUpdateOutput
        {
            AppliedToOpenDocument = false,
            Sql = query.QueryString,
            Executed = input.Execute,
            Problems = problems
        };
    }
}

public sealed class DbScriptOpenInput
{
    [Description("Script table")]
    public required DbScriptType Type { get; init; }

    [Description("Script id")]
    public required uint Id { get; init; }
}

[AutoRegister]
[SingleInstance]
public class DbScriptOpenTool : McpTool<DbScriptOpenInput, string>
{
    private readonly Lazy<IEventAggregator> eventAggregator;

    public DbScriptOpenTool(Lazy<IEventAggregator> eventAggregator)
    {
        this.eventAggregator = eventAggregator;
    }

    public override string Name => "dbscript_open";
    public override string Description => "Opens the dbscripts editor document for the given script in the editor UI.";
    public override bool Mutating => true;

    protected override Task<string> Execute(DbScriptOpenInput input, CancellationToken token)
    {
        eventAggregator.Value.GetEvent<EventRequestOpenItem>().Publish(new DbScriptSolutionItem(input.Type, input.Id));
        return Task.FromResult($"Opened {DbScriptTypes.GetInfo(input.Type).ReadableName} {input.Id}");
    }
}
