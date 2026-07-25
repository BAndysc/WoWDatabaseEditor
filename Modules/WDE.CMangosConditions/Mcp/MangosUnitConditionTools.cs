using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WDE.CMangosConditions.Data;
using WDE.CMangosConditions.ViewModels;
using WDE.Common.Database;
using WDE.Common.Services.Mcp;
using WDE.Module.Attributes;

namespace WDE.CMangosConditions.Mcp;

// unit_condition rows are shown exactly like in the editor: 8 flat clauses
// "variable op value" combined with AND (flags 0) or OR (flags 0x1).

public sealed class MangosUnitConditionTypesInput
{
    [Description("Optional case-insensitive substring filter over variable names")]
    public string? Filter { get; init; }

    [Description("Optional exact variable id")]
    public int? Id { get; init; }
}

public sealed class UnitConditionVariableInfo
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string ReadableName { get; init; }
    public string? Help { get; init; }

    [Description("Not implemented in the cmangos core - a clause with this variable evaluates to false")]
    public bool Nyi { get; init; }

    [Description("What the clause's Value column means; null = plain number")]
    public ConditionParamInfo? Value { get; init; }
}

public sealed class MangosUnitConditionTypesOutput
{
    [Description("How a unit_condition row works")]
    public required string Notes { get; init; }

    [Description("ConditionOperation values usable as a clause's op")]
    public required List<ConditionParamValueInfo> Operations { get; init; }

    public required List<UnitConditionVariableInfo> Variables { get; init; }
}

[AutoRegister]
[SingleInstance]
[RequiresCore(CmangosCoreTags.Wrath, CmangosCoreTags.Tbc, CmangosCoreTags.Classic)]
internal sealed class MangosUnitConditionTypesTool : McpTool<MangosUnitConditionTypesInput, MangosUnitConditionTypesOutput>
{
    private readonly Lazy<IUnitConditionDataManager> dataManager;

    public MangosUnitConditionTypesTool(Lazy<IUnitConditionDataManager> dataManager)
    {
        this.dataManager = dataManager;
    }

    public override string Name => "mangos_unit_condition_types";

    public override string Description => "Lists the cmangos unit_condition clause variable types (UnitCondition enum) with names, value meanings and help, plus the comparison operations, as the unit-condition editor presents them.";

    protected override Task<MangosUnitConditionTypesOutput> Execute(MangosUnitConditionTypesInput input, CancellationToken token)
    {
        var variables = dataManager.Value.AllVariables.AsEnumerable();
        if (input.Id.HasValue)
            variables = variables.Where(v => v.Id == input.Id.Value);
        if (!string.IsNullOrEmpty(input.Filter))
            variables = variables.Where(v => v.Name.Contains(input.Filter, StringComparison.OrdinalIgnoreCase) ||
                                             v.NameReadable.Contains(input.Filter, StringComparison.OrdinalIgnoreCase));

        var operations = (UnitConditionClauseViewModel.OpParameter.Items ?? new Dictionary<long, WDE.Common.Parameters.SelectOption>())
            .OrderBy(kv => kv.Key)
            .Select(kv => new ConditionParamValueInfo { Value = kv.Key, Name = kv.Value.Name, Description = kv.Value.Description })
            .ToList();

        return Task.FromResult(new MangosUnitConditionTypesOutput
        {
            Notes = "A unit_condition row has up to 8 clauses, each 'runtimeValue(variable, source unit, target unit) op value'. " +
                    "Variable 0 = unused clause (always true). Flags 0 = all clauses must pass (AND), flags 0x1 = any clause suffices (OR). " +
                    "Positive row ids are DBC-imported (wiped on reseed); custom rows use negative ids; -1 is the 'no condition' sentinel and must not be a row id.",
            Operations = operations,
            Variables = variables.OrderBy(v => v.Id).Select(v => new UnitConditionVariableInfo
            {
                Id = v.Id,
                Name = v.Name,
                ReadableName = v.NameReadable,
                Help = v.Help,
                Nyi = v.Nyi,
                Value = v.Value is { } value
                    ? new ConditionParamInfo
                    {
                        Name = value.Name,
                        Type = value.Type is { Length: > 0 } type ? type : "Parameter",
                        Description = value.Description,
                        Values = value.Values?.OrderBy(kv => kv.Key).Select(kv => new ConditionParamValueInfo
                        {
                            Value = kv.Key,
                            Name = kv.Value.Name,
                            Description = kv.Value.Description
                        }).ToList()
                    }
                    : null
            }).ToList()
        });
    }
}

public sealed class MangosUnitConditionGetInput
{
    [Description("unit_condition row id (signed: positive = DBC-imported, negative = custom rows)")]
    public required int Id { get; init; }
}

public sealed class UnitConditionClauseJson
{
    [Description("Clause slot 0..7")]
    public required int Index { get; init; }

    [Description("False for unused slots (variable 0, always true)")]
    public required bool Used { get; init; }

    public required int Variable { get; init; }
    public string? VariableName { get; init; }

    [Description("Comparison: none (always true), =, ≠, <, ≤, >, ≥")]
    public required string Op { get; init; }

    public required long Value { get; init; }

    [Description("Readable value (race/class/spell names etc.), when it differs from the number")]
    public string? ValueReadable { get; init; }

    [Description("The editor's readable text of the clause")]
    public required string Readable { get; init; }
}

public sealed class MangosUnitConditionGetOutput
{
    public required int Id { get; init; }

    [Description("How the clauses combine: AND (flags 0) or OR (flags 0x1)")]
    public required string CombineOperator { get; init; }

    public required uint Flags { get; init; }

    [Description("One-line readable text of the whole row, as the editor renders it")]
    public required string Readable { get; init; }

    [Description("All 8 clause slots exactly as the editor shows them")]
    public required List<UnitConditionClauseJson> Clauses { get; init; }
}

[AutoRegister]
[SingleInstance]
[RequiresCore(CmangosCoreTags.Wrath, CmangosCoreTags.Tbc, CmangosCoreTags.Classic)]
internal sealed class MangosUnitConditionGetTool : McpTool<MangosUnitConditionGetInput, MangosUnitConditionGetOutput>
{
    private readonly Lazy<IMangosDatabaseProvider> databaseProvider;
    private readonly Lazy<IUnitConditionClauseFactory> clauseFactory;
    private readonly Lazy<IUnitConditionDataManager> dataManager;

    public MangosUnitConditionGetTool(Lazy<IMangosDatabaseProvider> databaseProvider,
        Lazy<IUnitConditionClauseFactory> clauseFactory,
        Lazy<IUnitConditionDataManager> dataManager)
    {
        this.databaseProvider = databaseProvider;
        this.clauseFactory = clauseFactory;
        this.dataManager = dataManager;
    }

    public override string Name => "mangos_unit_condition_get";

    public override string Description => "Loads a cmangos unit_condition row and returns it the way the editor shows it: 8 clauses with named variables, comparison operators, readable values and a one-line readable summary (clauses combined with AND or OR).";

    protected override async Task<MangosUnitConditionGetOutput> Execute(MangosUnitConditionGetInput input, CancellationToken token)
    {
        var line = await databaseProvider.Value.GetUnitConditionById(input.Id)
                   ?? throw new McpToolException($"No unit_condition row with id {input.Id}");

        bool anyFlag = (line.Flags & 1) != 0;
        var combine = anyFlag ? "OR" : "AND";

        var clauses = new List<UnitConditionClauseJson>();
        var usedReadables = new List<string>();
        for (int i = 0; i < IMangosUnitConditionLine.ClausesCount; ++i)
        {
            var clause = line.GetClause(i);
            var vm = clauseFactory.Value.Create(clause);
            var valueReadable = vm.Value.String;
            var opName = UnitConditionClauseViewModel.OpParameter.Items is { } ops && ops.TryGetValue(clause.Op, out var option)
                ? option.Name
                : clause.Op.ToString(CultureInfo.InvariantCulture);

            if (!clause.IsNone)
                usedReadables.Add(vm.Readable);

            clauses.Add(new UnitConditionClauseJson
            {
                Index = i,
                Used = !clause.IsNone,
                Variable = (int)clause.Variable,
                VariableName = dataManager.Value.TryGetVariable((int)clause.Variable)?.Name,
                Op = opName,
                Value = clause.Value,
                ValueReadable = valueReadable != clause.Value.ToString(CultureInfo.InvariantCulture) ? valueReadable : null,
                Readable = vm.Readable
            });
        }

        return new MangosUnitConditionGetOutput
        {
            Id = line.Id,
            CombineOperator = combine,
            Flags = line.Flags,
            Readable = usedReadables.Count == 0
                ? "(no clauses - always true)"
                : string.Join($" {combine} ", usedReadables),
            Clauses = clauses
        };
    }
}
