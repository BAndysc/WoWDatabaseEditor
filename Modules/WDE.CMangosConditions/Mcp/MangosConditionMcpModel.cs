using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using WDE.CMangosConditions.Data;
using WDE.CMangosConditions.Models;
using WDE.CMangosConditions.ViewModels;
using WDE.Common.Database;
using WDE.Common.Services.Mcp;

namespace WDE.CMangosConditions.Mcp;

/// <summary>Core tags on which the cmangos conditions MCP tools are available.</summary>
internal static class CmangosCoreTags
{
    public const string Wrath = "CMaNGOS-WoTLK";
    public const string Tbc = "CMaNGOS-TBC";
    public const string Classic = "CMaNGOS-Classic";
}

// ---- shared DTOs (serialized with web defaults: camelCase) ----

public sealed class ConditionParamJson
{
    [Description("Parameter name as the editor shows it (see mangos_condition_types); 'value1'..'value4' also accepted on input")]
    public required string Name { get; init; }

    public required long Value { get; init; }

    [Description("Readable value (spell/item/quest names etc.); output only, ignored on input")]
    public string? Readable { get; init; }
}

public sealed class ConditionNodeJson
{
    [Description("condition_entry of this node in the database. Output: always set for saved rows. Input: optional edit-in-place hint, omit for new nodes")]
    public uint? Entry { get; init; }

    [Description("Condition type id: -1 AND, -2 OR, -3 NOT (combinators with children), 0..N leaf types (see mangos_condition_types). Input: either this or typeName")]
    public int? Type { get; init; }

    [Description("Condition type name, i.e. 'CONDITION_AURA' or 'Has aura'; 'AND'/'OR'/'NOT' also accepted (alternative to type on input)")]
    public string? TypeName { get; init; }

    [Description("The editor's readable text for this node; output only")]
    public string? Readable { get; init; }

    [Description("Negate the result (flag 0x1)")]
    public bool Negate { get; init; }

    [Description("Swap source and target before the check (flag 0x2), i.e. check on the source object instead of the target")]
    public bool SwapSourceAndTarget { get; init; }

    [Description("Row comment; when omitted the editor derives one from the readable text")]
    public string? Comment { get; init; }

    [Description("Leaf nodes only: named parameter values (only the parameters the type uses)")]
    public List<ConditionParamJson>? Params { get; init; }

    [Description("Combinator nodes only: nested conditions (NOT: exactly 1, AND/OR: 2..4)")]
    public List<ConditionNodeJson>? Children { get; init; }
}

public sealed class ConditionRowJson
{
    public required uint ConditionEntry { get; init; }
    public required int Type { get; init; }
    public required long Value1 { get; init; }
    public required long Value2 { get; init; }
    public required long Value3 { get; init; }
    public required long Value4 { get; init; }
    public required uint Flags { get; init; }
    public string? Comment { get; init; }
}

public sealed class ConditionParamValueInfo
{
    public required long Value { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
}

public sealed class ConditionParamInfo
{
    public required string Name { get; init; }
    [Description("Value type: a parameter key (spell/item/quest pickers etc.) or 'Parameter' for a plain number")]
    public required string Type { get; init; }
    public string? Description { get; init; }
    [Description("Defined enum values, when the parameter is an enum/flag choice")]
    public List<ConditionParamValueInfo>? Values { get; init; }
}

// ---- VM tree <-> JSON conversion, shared by the get/update tools ----

internal static class MangosConditionMcpModel
{
    public static ConditionNodeJson ToJsonNode(MangosConditionViewModel vm, IMangosConditionDataManager dataManager)
    {
        List<ConditionParamJson>? parameters = null;
        List<ConditionNodeJson>? children = null;

        if (vm.IsLogical)
        {
            children = vm.Children.Select(c => ToJsonNode(c, dataManager)).ToList();
        }
        else
        {
            parameters = new List<ConditionParamJson>();
            for (int i = 0; i < MangosConditionViewModel.ParametersCount; ++i)
            {
                var holder = vm.GetParameter(i);
                if (!holder.IsUsed && holder.Value == 0)
                    continue;
                var readable = holder.String;
                parameters.Add(new ConditionParamJson
                {
                    Name = holder.IsUsed && !string.IsNullOrEmpty(holder.Name) ? holder.Name : $"value{i + 1}",
                    Value = holder.Value,
                    Readable = readable != holder.Value.ToString() ? readable : null
                });
            }
        }

        return new ConditionNodeJson
        {
            Entry = vm.OriginalEntry != 0 ? vm.OriginalEntry : null,
            Type = vm.ConditionType,
            TypeName = dataManager.TryGetCondition(vm.ConditionType)?.Name,
            Readable = vm.GetReadable(withTags: false, withEntry: false),
            Negate = vm.Negate.Value != 0,
            SwapSourceAndTarget = vm.SwapTargets.Value != 0,
            Comment = string.IsNullOrEmpty(vm.Comment.Value) ? null : vm.Comment.Value,
            Params = parameters,
            Children = children
        };
    }

    /// <summary>Builds an editor view-model tree from the JSON shape, validating names against the type definitions.</summary>
    public static MangosConditionViewModel BuildVmNode(ConditionNodeJson json,
        IMangosConditionsFactory factory, IMangosConditionDataManager dataManager)
    {
        int type = ResolveTypeId(json, dataManager);
        var data = dataManager.TryGetCondition(type)
                   ?? throw new McpToolException($"Unknown condition type {type} (see mangos_condition_types)");

        var vm = factory.Create(type);
        if (json.Entry.HasValue)
            vm.OriginalEntry = json.Entry.Value;
        vm.Negate.Value = json.Negate ? 1 : 0;
        vm.SwapTargets.Value = json.SwapSourceAndTarget ? 1 : 0;
        vm.Comment.Value = json.Comment ?? "";

        if (data.IsLogical)
        {
            if (json.Params is { Count: > 0 })
                throw new McpToolException($"{data.Name} is a combinator: it takes 'children', not 'params'");
            foreach (var childJson in json.Children ?? new List<ConditionNodeJson>())
            {
                var child = BuildVmNode(childJson, factory, dataManager);
                child.Parent = vm;
                vm.Children.Add(child);
            }
        }
        else
        {
            if (json.Children is { Count: > 0 })
                throw new McpToolException($"{data.Name} is not AND/OR/NOT and cannot have 'children'");
            foreach (var param in json.Params ?? new List<ConditionParamJson>())
                AssignParameter(vm, data, param);
        }

        return vm;
    }

    private static int ResolveTypeId(ConditionNodeJson json, IMangosConditionDataManager dataManager)
    {
        if (json.Type.HasValue)
            return json.Type.Value;

        if (string.IsNullOrEmpty(json.TypeName))
            throw new McpToolException("Each node requires 'type' (id) or 'typeName' (see mangos_condition_types)");

        var byName = dataManager.AllConditions.FirstOrDefault(c =>
            string.Equals(c.Name, json.TypeName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(c.NameReadable, json.TypeName, StringComparison.OrdinalIgnoreCase));
        if (byName != null)
            return byName.Id;

        return json.TypeName.ToUpperInvariant() switch
        {
            "AND" => MangosConditionTreeCodec.TypeAnd,
            "OR" => MangosConditionTreeCodec.TypeOr,
            "NOT" => MangosConditionTreeCodec.TypeNot,
            _ => throw new McpToolException($"Unknown condition type '{json.TypeName}' (see mangos_condition_types)")
        };
    }

    private static void AssignParameter(MangosConditionViewModel vm, MangosConditionJson data, ConditionParamJson param)
    {
        for (int i = 0; i < MangosConditionViewModel.ParametersCount; ++i)
        {
            var holder = vm.GetParameter(i);
            bool byName = holder.IsUsed && string.Equals(holder.Name, param.Name, StringComparison.OrdinalIgnoreCase);
            bool bySlot = string.Equals(param.Name, $"value{i + 1}", StringComparison.OrdinalIgnoreCase);
            if (byName || bySlot)
            {
                holder.Value = param.Value;
                return;
            }
        }

        var known = new List<string>();
        for (int i = 0; i < MangosConditionViewModel.ParametersCount; ++i)
            if (vm.GetParameter(i).IsUsed)
                known.Add(vm.GetParameter(i).Name);
        throw new McpToolException(known.Count == 0
            ? $"Condition {data.Name} takes no parameters, but '{param.Name}' was given"
            : $"Condition {data.Name} has no parameter '{param.Name}'. Known parameters: {string.Join(", ", known)}");
    }

    public static ConditionRowJson ToRowJson(IMangosConditionLine line) => new()
    {
        ConditionEntry = line.ConditionEntry,
        Type = line.ConditionType,
        Value1 = line.Value1,
        Value2 = line.Value2,
        Value3 = line.Value3,
        Value4 = line.Value4,
        Flags = line.Flags,
        Comment = line.Comments
    };
}
