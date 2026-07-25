using System;
using System.Collections.Generic;
using System.Linq;
using WDE.Common.Database;
using WDE.Common.Services.Mcp;
using WDE.Common.Utils;
using WDE.Conditions.Data;
using WDE.Conditions.ViewModels;

namespace WDE.Conditions.Mcp
{
    /// <summary>
    /// Shared logic for the trinity_condition_* MCP tools: converts between raw `conditions`
    /// table rows and their editor-style JSON representation (named types, named parameters,
    /// readable sentences, else-groups as nested lists).
    /// </summary>
    internal static class ConditionsMcpModelBuilder
    {
        /// <summary>Same as ConditionViewModel.ParametersCount - ConditionValue1..3.</summary>
        public const int MaxParameters = 3;

        public static string? GetSourceTypeName(IConditionDataManager dataManager, int sourceType)
            => dataManager.HasConditionSourceData(sourceType) ? dataManager.GetConditionSourceData(sourceType).Name : null;

        /// <summary>
        /// The key mask determines which of SourceGroup/SourceEntry/SourceId identify the conditions
        /// of a source type (same rule the editor uses for its DELETE query).
        /// </summary>
        public static IDatabaseProvider.ConditionKeyMask GetMask(IConditionDataManager dataManager, int sourceType, bool sourceIdProvided)
        {
            if (dataManager.HasConditionSourceData(sourceType))
                return dataManager.GetConditionSourceData(sourceType).GetMask();
            var mask = IDatabaseProvider.ConditionKeyMask.SourceGroup | IDatabaseProvider.ConditionKeyMask.SourceEntry;
            if (sourceIdProvided)
                mask |= IDatabaseProvider.ConditionKeyMask.SourceId;
            return mask;
        }

        /// <summary>Groups rows by ElseGroup (rows in one group are ANDed, groups are ORed).</summary>
        public static List<List<McpConditionJson>> GroupConditions(int sourceType, IEnumerable<ICondition> lines,
            IConditionsFactory factory, IConditionDataManager dataManager)
        {
            return lines
                .GroupBy(l => l.ElseGroup)
                .OrderBy(g => g.Key)
                .Select(g => g.Select(l => ConditionToJson(sourceType, l, factory, dataManager)).ToList())
                .ToList();
        }

        public static McpConditionJson ConditionToJson(int sourceType, ICondition line,
            IConditionsFactory factory, IConditionDataManager dataManager)
        {
            ConditionJsonData? data = dataManager.HasConditionData(line.ConditionType)
                ? dataManager.GetConditionData(line.ConditionType)
                : null;
            var vm = factory.Create(sourceType, line);

            List<McpConditionParameterJson>? parameters = null;
            for (int i = 0; i < MaxParameters; ++i)
            {
                bool defined = data?.Parameters != null && i < data.Parameters.Count;
                long value = line.GetConditionValue(i);
                if (!defined && value == 0)
                    continue;

                string? valueName = null;
                if (vm != null)
                {
                    var asString = SafeParameterToString(vm, i);
                    if (asString != null && asString != value.ToString())
                        valueName = asString;
                }

                parameters ??= new List<McpConditionParameterJson>();
                parameters.Add(new McpConditionParameterJson
                {
                    Name = defined ? data!.Parameters![i].Name : null,
                    Value = value,
                    ValueName = valueName
                });
            }

            bool stringDefined = data?.StringParameters is { Count: > 0 };
            string? stringParam = stringDefined || !string.IsNullOrEmpty(line.ConditionStringValue1)
                ? line.ConditionStringValue1
                : null;

            return new McpConditionJson
            {
                Type = data?.Name,
                TypeId = line.ConditionType,
                Negated = line.NegativeCondition != 0,
                Target = line.ConditionTarget,
                TargetLabel = GetTargetLabel(dataManager, sourceType, line.ConditionTarget),
                Parameters = parameters,
                StringParameter = stringParam,
                Readable = SafeReadable(vm),
                Comment = line.Comment
            };
        }

        public static string? GetTargetLabel(IConditionDataManager dataManager, int sourceType, int target)
        {
            if (!dataManager.HasConditionSourceData(sourceType))
                return null;
            var source = dataManager.GetConditionSourceData(sourceType);
            if (source.Targets is { } targets && targets.TryGetValue(target, out var t))
                return t.Description;
            return null;
        }

        /// <summary>One human sentence for the whole set: groups joined with OR, conditions with AND.</summary>
        public static string? BuildOverallReadable(List<List<McpConditionJson>> groups)
        {
            if (groups.Count == 0)
                return null;
            return string.Join("  OR  ", groups.Select(g =>
                "(" + string.Join(" AND ", g.Select(c => c.Readable ?? c.Type ?? $"type {c.TypeId}")) + ")"));
        }

        /// <summary>
        /// Definition-based validation (the conditions module has no dedicated validator service):
        /// unknown types, parameter counts/names/enum values, targets and string parameter support.
        /// </summary>
        public static List<McpConditionProblemJson> Validate(int sourceType, List<List<McpConditionJson>>? groups,
            IConditionDataManager dataManager, bool coreHasStringValue)
        {
            var problems = new List<McpConditionProblemJson>();

            void Add(McpConditionProblemSeverity severity, int? group, int? index, string message)
                => problems.Add(new McpConditionProblemJson { Severity = severity, Group = group, Index = index, Message = message });

            bool sourceKnown = dataManager.HasConditionSourceData(sourceType);
            if (!sourceKnown)
                Add(McpConditionProblemSeverity.Warning, null, null,
                    $"Source type {sourceType} is not known to the editor's condition source definitions; targets and key column meanings cannot be verified.");

            if (groups == null || groups.Count == 0)
            {
                Add(McpConditionProblemSeverity.Warning, null, null,
                    "No condition groups given - an update with this input would just delete all conditions for the source.");
                return problems;
            }

            for (int g = 0; g < groups.Count; ++g)
            {
                var group = groups[g];
                if (group == null! || group.Count == 0)
                {
                    Add(McpConditionProblemSeverity.Warning, g, null, "The group is empty and will produce no rows.");
                    continue;
                }

                for (int i = 0; i < group.Count; ++i)
                {
                    var cond = group[i];
                    if (cond == null!)
                    {
                        Add(McpConditionProblemSeverity.Error, g, i, "Condition entry is null.");
                        continue;
                    }

                    ConditionJsonData? data = null;
                    if (cond.Type != null)
                    {
                        if (!dataManager.HasConditionData(cond.Type))
                            Add(McpConditionProblemSeverity.Error, g, i,
                                $"Unknown condition type '{cond.Type}'. Use trinity_condition_types to list valid CONDITION_* names.");
                        else
                        {
                            data = dataManager.GetConditionData(cond.Type);
                            if (cond.TypeId.HasValue && cond.TypeId.Value != data.Id)
                                Add(McpConditionProblemSeverity.Warning, g, i,
                                    $"typeId {cond.TypeId} is ignored because it does not match type '{cond.Type}' (id {data.Id}).");
                        }
                    }
                    else if (cond.TypeId.HasValue)
                    {
                        if (!dataManager.HasConditionData(cond.TypeId.Value))
                            Add(McpConditionProblemSeverity.Error, g, i,
                                $"Unknown condition type id {cond.TypeId.Value}. Use trinity_condition_types to list valid types.");
                        else
                            data = dataManager.GetConditionData(cond.TypeId.Value);
                    }
                    else
                        Add(McpConditionProblemSeverity.Error, g, i, "Each condition requires 'type' (CONDITION_* name) or 'typeId'.");

                    if (data != null && data.Id == -1)
                    {
                        Add(McpConditionProblemSeverity.Error, g, i,
                            "CONDITION_LOGICAL_OR is a UI-only marker; express OR by putting conditions into separate groups instead.");
                        continue;
                    }

                    if (cond.Target < 0 || cond.Target > 255)
                        Add(McpConditionProblemSeverity.Error, g, i, $"Target {cond.Target} is out of the 0-255 range.");
                    else if (sourceKnown)
                    {
                        var source = dataManager.GetConditionSourceData(sourceType);
                        if (source.Targets is { Count: > 0 } targets && !targets.ContainsKey(cond.Target))
                            Add(McpConditionProblemSeverity.Error, g, i,
                                $"Target {cond.Target} is not valid for {source.Name}; valid targets: " +
                                string.Join(", ", targets.OrderBy(t => t.Key).Select(t => $"{t.Key} = {t.Value.Description}")));
                    }

                    if (data == null)
                        continue;

                    int definedCount = data.Parameters?.Count ?? 0;
                    if (cond.Parameters != null)
                    {
                        if (cond.Parameters.Count > MaxParameters)
                            Add(McpConditionProblemSeverity.Error, g, i,
                                $"{data.Name}: at most {MaxParameters} parameters are supported, got {cond.Parameters.Count}.");

                        for (int p = 0; p < Math.Min(cond.Parameters.Count, MaxParameters); ++p)
                        {
                            var param = cond.Parameters[p];
                            if (param == null!)
                            {
                                Add(McpConditionProblemSeverity.Error, g, i, "Parameter entry is null.");
                                continue;
                            }

                            int index = ResolveParameterIndex(data, param, p, out var error);
                            if (error != null)
                            {
                                Add(McpConditionProblemSeverity.Error, g, i, error);
                                continue;
                            }

                            if (index >= definedCount)
                            {
                                if (param.Value != 0)
                                    Add(McpConditionProblemSeverity.Warning, g, i,
                                        $"{data.Name} defines only {definedCount} parameter(s), the value {param.Value} in slot {index + 1} has no meaning.");
                                continue;
                            }

                            var definition = data.Parameters![index];
                            if (definition.Values is { Count: > 0 } options && param.Value != 0 && !options.ContainsKey(param.Value))
                            {
                                // could be a flag combination - accept any value covered by OR-ing all defined bits
                                long allBits = 0;
                                foreach (var key in options.Keys)
                                    allBits |= key;
                                if ((param.Value & ~allBits) != 0)
                                    Add(McpConditionProblemSeverity.Warning, g, i,
                                        $"{data.Name}.{definition.Name}: value {param.Value} is not among the defined options: " +
                                        string.Join(", ", options.OrderBy(o => o.Key).Select(o => $"{o.Key} = {o.Value.Name}")));
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(cond.StringParameter))
                    {
                        if (data.StringParameters is not { Count: > 0 })
                            Add(McpConditionProblemSeverity.Warning, g, i,
                                $"{data.Name} does not define a string parameter, 'stringParameter' will be ignored.");
                        else if (!coreHasStringValue)
                            Add(McpConditionProblemSeverity.Warning, g, i,
                                "The current core does not store ConditionStringValue1, 'stringParameter' will not be exported.");
                    }
                }
            }

            return problems;
        }

        /// <summary>Builds full condition lines (ready for SQL export) from the JSON groups. Throws McpToolException on structural errors.</summary>
        public static List<IConditionLine> BuildConditionLines(int sourceType, long sourceGroup, int sourceEntry, int sourceId,
            List<List<McpConditionJson>> groups, IConditionsFactory factory, IConditionDataManager dataManager)
        {
            var result = new List<IConditionLine>();
            for (int g = 0; g < groups.Count; ++g)
            {
                if (groups[g] == null!)
                    throw new McpToolException($"Group {g} is null.");
                foreach (var json in groups[g])
                {
                    if (json == null!)
                        throw new McpToolException($"Group {g} contains a null condition.");
                    var condition = BuildCondition(sourceType, json, g, factory, dataManager);
                    result.Add(new AbstractConditionLine(sourceType, sourceGroup, sourceEntry, sourceId, condition));
                }
            }
            return result;
        }

        public static AbstractCondition BuildCondition(int sourceType, McpConditionJson json, int elseGroup,
            IConditionsFactory factory, IConditionDataManager dataManager)
        {
            int typeId = ResolveTypeId(json, dataManager);
            var data = dataManager.GetConditionData(typeId);

            if (json.Target < 0 || json.Target > 255)
                throw new McpToolException($"Condition {data.Name}: target {json.Target} is out of the 0-255 range.");

            Span<long> values = stackalloc long[MaxParameters];
            if (json.Parameters != null)
            {
                if (json.Parameters.Count > MaxParameters)
                    throw new McpToolException($"Condition {data.Name}: at most {MaxParameters} parameters are supported, got {json.Parameters.Count}.");

                for (int i = 0; i < json.Parameters.Count; ++i)
                {
                    var param = json.Parameters[i];
                    if (param == null!)
                        throw new McpToolException($"Condition {data.Name}: parameter {i + 1} is null.");
                    int index = ResolveParameterIndex(data, param, i, out var error);
                    if (error != null)
                        throw new McpToolException(error);
                    values[index] = param.Value;
                }
            }

            var condition = new AbstractCondition
            {
                ElseGroup = elseGroup,
                ConditionType = typeId,
                ConditionTarget = (byte)json.Target,
                ConditionValue1 = values[0],
                ConditionValue2 = values[1],
                ConditionValue3 = values[2],
                ConditionStringValue1 = json.StringParameter ?? "",
                NegativeCondition = json.Negated ? 1 : 0
            };

            // like the editor, the comment defaults to the readable sentence
            var comment = json.Comment ?? SafeReadable(factory.Create(sourceType, condition));
            return new AbstractCondition(condition) { Comment = comment };
        }

        public static int ResolveTypeId(McpConditionJson json, IConditionDataManager dataManager)
        {
            if (json.Type != null)
            {
                if (!dataManager.HasConditionData(json.Type))
                    throw new McpToolException($"Unknown condition type '{json.Type}'. Use trinity_condition_types to list valid CONDITION_* names.");
                var data = dataManager.GetConditionData(json.Type);
                ThrowIfLogicalOr(data.Id);
                return data.Id;
            }

            if (json.TypeId.HasValue)
            {
                ThrowIfLogicalOr(json.TypeId.Value);
                if (!dataManager.HasConditionData(json.TypeId.Value))
                    throw new McpToolException($"Unknown condition type id {json.TypeId.Value}. Use trinity_condition_types to list valid types.");
                return json.TypeId.Value;
            }

            throw new McpToolException("Each condition requires 'type' (CONDITION_* name) or 'typeId'.");
        }

        private static void ThrowIfLogicalOr(int typeId)
        {
            if (typeId == -1)
                throw new McpToolException("CONDITION_LOGICAL_OR is a UI-only marker; express OR by putting conditions into separate groups instead.");
        }

        private static int ResolveParameterIndex(ConditionJsonData data, McpConditionParameterJson param, int positionalIndex, out string? error)
        {
            error = null;
            if (param.Name == null)
                return positionalIndex;

            var defined = data.Parameters;
            if (defined != null)
            {
                for (int j = 0; j < defined.Count; ++j)
                {
                    if (string.Equals(defined[j].Name, param.Name, StringComparison.OrdinalIgnoreCase))
                        return j;
                }
            }

            error = $"Unknown parameter name '{param.Name}' for {data.Name}; defined parameters: " +
                    (defined is { Count: > 0 } ? string.Join(", ", defined.Select(p => p.Name)) : "(none)");
            return positionalIndex;
        }

        private static string? SafeReadable(ConditionViewModel? vm)
        {
            if (vm == null)
                return null;
            try
            {
                return vm.Readable.RemoveTags();
            }
            catch (Exception)
            {
                return null; // a malformed description template must not break the tool
            }
        }

        private static string? SafeParameterToString(ConditionViewModel vm, int index)
        {
            try
            {
                return vm.GetParameter(index).ToString();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
