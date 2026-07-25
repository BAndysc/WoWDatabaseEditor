using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WDE.CMangosConditions.Data;
using WDE.CMangosConditions.Models;
using WDE.CMangosConditions.ViewModels;
using WDE.Common.Database;
using WDE.Common.Services;
using WDE.Common.Services.IdGenerator;
using WDE.Common.Services.Mcp;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.CMangosConditions.Mcp;

// The MCP surface deliberately mirrors the EDITOR's representation of mangos conditions:
// nested AND/OR/NOT trees with named, readable leaf parameters - never raw column dumps.

public sealed class MangosConditionTypesInput
{
    [Description("Optional case-insensitive substring filter over type names")]
    public string? Filter { get; init; }

    [Description("Optional exact type id (may be negative for the AND/OR/NOT combinators)")]
    public int? Id { get; init; }
}

public sealed class ConditionTypeInfo
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string ReadableName { get; init; }
    public string? Help { get; init; }

    [Description("True for the AND (-1) / OR (-2) / NOT (-3) pseudo-types: they take nested child conditions instead of parameters, forming trees")]
    public required bool IsCombinator { get; init; }

    [Description("Combinators only: allowed number of nested conditions")]
    public int MinChildren { get; init; }
    public int MaxChildren { get; init; }

    [Description("What kind of object the condition is checked on (Player, Unit, WorldObject, Map, SourceCreature)")]
    public string? Subject { get; init; }

    [Description("Named parameters, in value1..value4 order")]
    public List<ConditionParamInfo>? Parameters { get; init; }
}

[AutoRegister]
[SingleInstance]
[RequiresCore(CmangosCoreTags.Wrath, CmangosCoreTags.Tbc, CmangosCoreTags.Classic)]
internal sealed class MangosConditionTypesTool : McpTool<MangosConditionTypesInput, List<ConditionTypeInfo>>
{
    private readonly Lazy<IMangosConditionDataManager> dataManager;

    public MangosConditionTypesTool(Lazy<IMangosConditionDataManager> dataManager)
    {
        this.dataManager = dataManager;
    }

    public override string Name => "mangos_condition_types";

    public override string Description => "Lists the cmangos `conditions` table condition types with named parameters, enum values and help, as the conditions editor presents them. Includes the AND (-1) / OR (-2) / NOT (-3) combinator pseudo-types whose rows reference child condition entries via value1..value4, forming trees. Use the parameter names with mangos_condition_update.";

    protected override Task<List<ConditionTypeInfo>> Execute(MangosConditionTypesInput input, CancellationToken token)
    {
        var conditions = dataManager.Value.AllConditions.AsEnumerable();
        if (input.Id.HasValue)
            conditions = conditions.Where(c => c.Id == input.Id.Value);
        if (!string.IsNullOrEmpty(input.Filter))
            conditions = conditions.Where(c => c.Name.Contains(input.Filter, StringComparison.OrdinalIgnoreCase) ||
                                               c.NameReadable.Contains(input.Filter, StringComparison.OrdinalIgnoreCase));

        var result = conditions.OrderBy(c => c.Id).Select(c => new ConditionTypeInfo
        {
            Id = c.Id,
            Name = c.Name,
            ReadableName = c.NameReadable,
            Help = c.Help,
            IsCombinator = c.IsLogical,
            MinChildren = c.MinChildren,
            MaxChildren = c.MaxChildren,
            Subject = c.Subject == MangosConditionSubject.None ? null : c.Subject.ToString(),
            Parameters = c.Parameters?.Select(p => new ConditionParamInfo
            {
                Name = p.Name,
                Type = p.Type is { Length: > 0 } type ? type : "Parameter",
                Description = p.Description,
                Values = p.Values?.OrderBy(kv => kv.Key).Select(kv => new ConditionParamValueInfo
                {
                    Value = kv.Key,
                    Name = kv.Value.Name,
                    Description = kv.Value.Description
                }).ToList()
            }).ToList()
        }).ToList();
        return Task.FromResult(result);
    }
}

public sealed class MangosConditionGetInput
{
    [Description("condition_entry of the tree root to load")]
    public required uint ConditionId { get; init; }
}

public sealed class MangosConditionGetOutput
{
    public required uint ConditionId { get; init; }

    [Description("One-line readable text of the whole tree, as the editor renders it")]
    public required string Readable { get; init; }

    [Description("The condition tree as the editor shows it; the same shape is accepted by mangos_condition_update")]
    public required ConditionNodeJson Tree { get; init; }

    [Description("Raw `conditions` rows of the loaded closure (secondary; the tree is the primary representation)")]
    public required List<ConditionRowJson> Rows { get; init; }
}

[AutoRegister]
[SingleInstance]
[RequiresCore(CmangosCoreTags.Wrath, CmangosCoreTags.Tbc, CmangosCoreTags.Classic)]
internal sealed class MangosConditionGetTool : McpTool<MangosConditionGetInput, MangosConditionGetOutput>
{
    private readonly Lazy<IMangosConditionService> conditionService;
    private readonly Lazy<IMangosConditionsFactory> factory;
    private readonly Lazy<IMangosConditionDataManager> dataManager;

    public MangosConditionGetTool(Lazy<IMangosConditionService> conditionService,
        Lazy<IMangosConditionsFactory> factory,
        Lazy<IMangosConditionDataManager> dataManager)
    {
        this.conditionService = conditionService;
        this.factory = factory;
        this.dataManager = dataManager;
    }

    public override string Name => "mangos_condition_get";

    public override string Description => "Loads a cmangos `conditions` tree (the given condition_entry plus everything it transitively references) and returns it the way the editor shows it: nested AND/OR/NOT nodes with named leaf parameters, readable values and a one-line readable summary.";

    protected override async Task<MangosConditionGetOutput> Execute(MangosConditionGetInput input, CancellationToken token)
    {
        if (input.ConditionId == 0)
            throw new McpToolException("conditionId must be a positive condition_entry (0 means 'no condition')");

        var closure = await conditionService.Value.LoadConditionsClosure(input.ConditionId);
        if (closure.Count == 0)
            throw new McpToolException($"No condition with entry {input.ConditionId} in the conditions table");

        var roots = MangosConditionTreeCodec.BuildTree(closure, factory.Value);
        var root = roots.FirstOrDefault(r => r.OriginalEntry == input.ConditionId)
                   ?? throw new McpToolException($"Condition {input.ConditionId} could not be resolved as a tree root (broken data?)");

        return new MangosConditionGetOutput
        {
            ConditionId = input.ConditionId,
            Readable = conditionService.Value.BuildReadable(input.ConditionId, closure),
            Tree = MangosConditionMcpModel.ToJsonNode(root, dataManager.Value),
            Rows = closure.Select(MangosConditionMcpModel.ToRowJson).ToList()
        };
    }
}

public sealed class MangosConditionSearchInput
{
    [Description("Value to look for in value1..value4 (i.e. a spell, item, quest or condition id)")]
    public required long Value { get; init; }

    [Description("Maximum number of matches to return (default 50)")]
    public int Limit { get; init; } = 50;
}

public sealed class MangosConditionSearchMatch
{
    public required uint ConditionEntry { get; init; }
    public required int Type { get; init; }
    public string? TypeName { get; init; }

    [Description("Which value slots matched, with the parameter name the slot has for this type")]
    public required List<string> MatchedIn { get; init; }

    [Description("The editor's readable text of this row (combinators list their child condition refs)")]
    public required string Readable { get; init; }

    public string? Comment { get; init; }
}

public sealed class MangosConditionSearchOutput
{
    public required List<MangosConditionSearchMatch> Matches { get; init; }
    public required bool Truncated { get; init; }
}

[AutoRegister]
[SingleInstance]
[RequiresCore(CmangosCoreTags.Wrath, CmangosCoreTags.Tbc, CmangosCoreTags.Classic)]
internal sealed class MangosConditionSearchTool : McpTool<MangosConditionSearchInput, MangosConditionSearchOutput>
{
    private readonly Lazy<IMySqlExecutor> executor;
    private readonly Lazy<IMangosConditionDataManager> dataManager;
    private readonly Lazy<IMangosConditionsFactory> factory;

    public MangosConditionSearchTool(Lazy<IMySqlExecutor> executor,
        Lazy<IMangosConditionDataManager> dataManager,
        Lazy<IMangosConditionsFactory> factory)
    {
        this.executor = executor;
        this.dataManager = dataManager;
        this.factory = factory;
    }

    public override string Name => "mangos_condition_search";

    public override string Description => "Finds `conditions` rows whose value1..value4 reference the given value (i.e. all conditions about a spell, item, quest - or all combinators referencing a condition entry). Returns readable matches; use mangos_condition_get for the full tree of a match.";

    protected override async Task<MangosConditionSearchOutput> Execute(MangosConditionSearchInput input, CancellationToken token)
    {
        if (!executor.Value.IsConnected)
            throw new McpToolException("World database is not connected");

        var limit = Math.Clamp(input.Limit, 1, 1000);
        var value = input.Value.ToString(CultureInfo.InvariantCulture);
        var sql = $"SELECT condition_entry, type, value1, value2, value3, value4, flags, comments FROM conditions " +
                  $"WHERE value1 = {value} OR value2 = {value} OR value3 = {value} OR value4 = {value} " +
                  $"ORDER BY condition_entry LIMIT {limit + 1}";

        IDatabaseSelectResult result;
        try
        {
            result = await executor.Value.ExecuteSelectSql(sql);
        }
        catch (IMySqlExecutor.DatabaseExecutorException e)
        {
            throw new McpToolException(e.InnerException?.Message ?? e.Message);
        }

        int entryCol = result.ColumnIndex("condition_entry");
        int typeCol = result.ColumnIndex("type");
        int[] valueCols =
        {
            result.ColumnIndex("value1"), result.ColumnIndex("value2"),
            result.ColumnIndex("value3"), result.ColumnIndex("value4")
        };
        int commentsCol = result.ColumnIndex("comments");

        var matches = new List<MangosConditionSearchMatch>();
        bool truncated = false;
        foreach (var row in result)
        {
            if (matches.Count >= limit)
            {
                truncated = true;
                break;
            }

            var line = new AbstractMangosConditionLine
            {
                ConditionEntry = Convert.ToUInt32(result.Value(row, entryCol), CultureInfo.InvariantCulture),
                ConditionType = Convert.ToInt32(result.Value(row, typeCol), CultureInfo.InvariantCulture),
                Value1 = Convert.ToUInt32(result.Value(row, valueCols[0]), CultureInfo.InvariantCulture),
                Value2 = Convert.ToUInt32(result.Value(row, valueCols[1]), CultureInfo.InvariantCulture),
                Value3 = Convert.ToUInt32(result.Value(row, valueCols[2]), CultureInfo.InvariantCulture),
                Value4 = Convert.ToUInt32(result.Value(row, valueCols[3]), CultureInfo.InvariantCulture),
                Comments = result.IsNull(row, commentsCol) ? null : result.Value(row, commentsCol)?.ToString()
            };

            var data = dataManager.Value.TryGetCondition(line.ConditionType);
            bool logical = MangosConditionTreeCodec.IsLogicalType(line.ConditionType);
            var vm = logical ? null : factory.Value.Create(line);

            var values = new[] { line.Value1, line.Value2, line.Value3, line.Value4 };
            var matchedIn = new List<string>();
            for (int i = 0; i < values.Length; ++i)
            {
                if (values[i] != input.Value)
                    continue;
                if (logical)
                    matchedIn.Add($"value{i + 1} (child condition ref)");
                else
                {
                    var holder = vm!.GetParameter(i);
                    matchedIn.Add(holder.IsUsed && !string.IsNullOrEmpty(holder.Name)
                        ? $"value{i + 1} ({holder.Name})"
                        : $"value{i + 1}");
                }
            }

            var readable = logical
                ? (data?.NameReadable ?? $"combinator {line.ConditionType}") + ": " +
                  string.Join(", ", MangosConditionTreeCodec.ChildRefs(line).Select(r => $"#{r}"))
                : vm!.GetReadable(withTags: false, withEntry: false);

            matches.Add(new MangosConditionSearchMatch
            {
                ConditionEntry = line.ConditionEntry,
                Type = line.ConditionType,
                TypeName = data?.Name,
                MatchedIn = matchedIn,
                Readable = readable,
                Comment = line.Comments
            });
        }

        return new MangosConditionSearchOutput { Matches = matches, Truncated = truncated };
    }
}

public sealed class MangosConditionUpdateInput
{
    [Description("condition_entry of the existing tree root to replace, or 0 to create a new condition tree")]
    public required uint ConditionId { get; init; }

    [Description("The new tree (same shape as mangos_condition_get returns): combinator nodes with children, leaf nodes with type + named params")]
    public required ConditionNodeJson Tree { get; init; }

    [Description("When true the generated SQL is executed against the world database; default false = only return the SQL for review")]
    public bool Execute { get; init; }
}

public sealed class MangosConditionUpdateOutput
{
    [Description("condition_entry of the saved tree root (may differ from conditionId when entries had to be reassigned)")]
    public required uint RootEntry { get; init; }

    [Description("One-line readable text of the resulting tree")]
    public required string Readable { get; init; }

    [Description("The same delete+insert SQL the editor's save produces")]
    public required string Sql { get; init; }

    public required bool Executed { get; init; }
}

[AutoRegister]
[SingleInstance]
[RequiresCore(CmangosCoreTags.Wrath, CmangosCoreTags.Tbc, CmangosCoreTags.Classic)]
internal sealed class MangosConditionUpdateTool : McpTool<MangosConditionUpdateInput, MangosConditionUpdateOutput>
{
    private readonly Lazy<IMangosConditionService> conditionService;
    private readonly Lazy<IMangosConditionsFactory> factory;
    private readonly Lazy<IMangosConditionDataManager> dataManager;
    private readonly Lazy<IMangosConditionQueryGenerator> queryGenerator;
    private readonly Lazy<IMySqlExecutor> executor;
    private readonly Lazy<IIdGeneratorService> idGenerator;

    public MangosConditionUpdateTool(Lazy<IMangosConditionService> conditionService,
        Lazy<IMangosConditionsFactory> factory,
        Lazy<IMangosConditionDataManager> dataManager,
        Lazy<IMangosConditionQueryGenerator> queryGenerator,
        Lazy<IMySqlExecutor> executor,
        Lazy<IIdGeneratorService> idGenerator)
    {
        this.conditionService = conditionService;
        this.factory = factory;
        this.dataManager = dataManager;
        this.queryGenerator = queryGenerator;
        this.executor = executor;
        this.idGenerator = idGenerator;
    }

    public override string Name => "mangos_condition_update";

    public override string Description => "Builds or replaces a cmangos `conditions` tree from the editor-style JSON shape (same as mangos_condition_get returns) and generates the same delete+insert SQL the editor's save produces (children always get lower entries than parents, identical rows collapse). Pass execute=true to apply it to the world database.";

    public override bool Mutating => true;

    protected override async Task<MangosConditionUpdateOutput> Execute(MangosConditionUpdateInput input, CancellationToken token)
    {
        var closure = input.ConditionId == 0
            ? Array.Empty<IMangosConditionLine>()
            : await conditionService.Value.LoadConditionsClosure(input.ConditionId);
        if (input.ConditionId != 0 && closure.Count == 0)
            throw new McpToolException($"No condition with entry {input.ConditionId} in the conditions table (pass conditionId=0 to create a new tree)");

        var root = MangosConditionMcpModel.BuildVmNode(input.Tree, factory.Value, dataManager.Value);
        var roots = new List<MangosConditionViewModel> { root };
        var errors = MangosConditionTreeCodec.Validate(roots);
        if (errors.Count > 0)
            throw new McpToolException("Invalid condition tree:\n" + string.Join("\n", errors));

        // new nodes must get entries above everything we know about: the loaded closure,
        // entries passed in the tree, and the database maximum (via the id generator)
        uint localMax = closure.Count == 0 ? 0 : closure.Max(c => c.ConditionEntry);
        foreach (var node in root.Descendants())
            localMax = Math.Max(localMax, node.OriginalEntry);
        uint firstFree = await conditionService.Value.GetFirstFreeConditionEntry(localMax);

        var serialized = MangosConditionTreeCodec.Serialize(roots, firstFree);
        if (serialized.Lines.Count > 0)
        {
            uint maxAssigned = serialized.Lines.Max(l => l.ConditionEntry);
            if (maxAssigned >= firstFree)
                idGenerator.Value.MarkUsed(new MangosConditionEntryIdType(), firstFree, maxAssigned);
        }

        var affected = closure.Select(l => l.ConditionEntry)
            .Concat(serialized.Lines.Select(l => l.ConditionEntry))
            .Distinct()
            .ToList();

        var transaction = Queries.BeginTransaction(DataDatabaseType.World);
        transaction.Add(queryGenerator.Value.BuildDeleteQuery(affected));
        transaction.Add(queryGenerator.Value.BuildInsertQuery(serialized.Lines));
        var query = transaction.Close();

        if (input.Execute)
        {
            try
            {
                await executor.Value.ExecuteSql(query);
            }
            catch (IMySqlExecutor.DatabaseExecutorException e)
            {
                throw new McpToolException(e.InnerException?.Message ?? e.Message);
            }
        }

        uint rootEntry = serialized.RootEntries.Count > 0 ? serialized.RootEntries[0] : 0;
        return new MangosConditionUpdateOutput
        {
            RootEntry = rootEntry,
            Readable = conditionService.Value.BuildReadable(rootEntry, serialized.Lines),
            Sql = query.QueryString,
            Executed = input.Execute
        };
    }
}
