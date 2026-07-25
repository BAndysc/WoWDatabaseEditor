using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Parameters;
using WDE.Common.Services.Mcp;
using WDE.Module.Attributes;

namespace WDE.Mcp.Tools.Parameters;

public sealed class ParameterListInput
{
    [Description("Optional case-insensitive substring filter over parameter keys (i.e. 'spell', 'faction')")]
    public string? Filter { get; init; }
}

[AutoRegister]
[SingleInstance]
public class ParameterListTool : McpTool<ParameterListInput, List<string>>
{
    private readonly IParameterFactory parameterFactory;

    public ParameterListTool(IParameterFactory parameterFactory)
    {
        this.parameterFactory = parameterFactory;
    }

    public override string Name => "parameter_list";
    public override string Description => "Lists registered parameter keys (named value spaces like SpellParameter, FactionParameter, flags and enums used across the editor). Use parameter_search to look up values by name.";

    protected override Task<List<string>> Execute(ParameterListInput input, CancellationToken token)
    {
        var keys = parameterFactory.GetKeys();
        if (!string.IsNullOrEmpty(input.Filter))
            keys = keys.Where(k => k.Contains(input.Filter, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(keys.OrderBy(x => x).ToList());
    }
}

public sealed class ParameterSearchInput
{
    [Description("Parameter key, i.e. 'SpellParameter' (see parameter_list)")]
    public required string Key { get; init; }

    [Description("Case-insensitive regex matched against the item names (also matched literally against values)")]
    public required string Regex { get; init; }

    [Description("Maximum number of matches to return (default 50)")]
    public int Limit { get; init; } = 50;
}

public sealed class ParameterItem
{
    public required long Value { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
}

[AutoRegister]
[SingleInstance]
public class ParameterSearchTool : McpTool<ParameterSearchInput, List<ParameterItem>>
{
    private readonly IParameterFactory parameterFactory;

    public ParameterSearchTool(IParameterFactory parameterFactory)
    {
        this.parameterFactory = parameterFactory;
    }

    public override string Name => "parameter_search";
    public override string Description => "Searches a parameter's known values by regex over their names (i.e. find a spell id by spell name). Returns value + name pairs.";

    protected override Task<List<ParameterItem>> Execute(ParameterSearchInput input, CancellationToken token)
    {
        if (!parameterFactory.IsRegisteredLong(input.Key))
            throw new McpToolException($"Unknown parameter key: {input.Key} (see parameter_list)");

        var parameter = parameterFactory.Factory(input.Key);
        if (parameter.Items == null || parameter.Items.Count == 0)
            return Task.FromResult(new List<ParameterItem>());

        Regex regex;
        try
        {
            regex = new Regex(input.Regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(2));
        }
        catch (ArgumentException e)
        {
            throw new McpToolException($"Invalid regex: {e.Message}");
        }

        var limit = Math.Max(1, input.Limit);
        var results = new List<ParameterItem>();
        foreach (var (value, option) in parameter.Items)
        {
            if (results.Count >= limit)
                break;
            if (regex.IsMatch(option.Name) || value.ToString() == input.Regex)
                results.Add(new ParameterItem { Value = value, Name = option.Name, Description = option.Description });
        }
        return Task.FromResult(results);
    }
}

public sealed class ParameterValueNameInput
{
    [Description("Parameter key, i.e. 'SpellParameter'")]
    public required string Key { get; init; }

    [Description("The numeric value to resolve to a readable name")]
    public required long Value { get; init; }
}

[AutoRegister]
[SingleInstance]
public class ParameterValueNameTool : McpTool<ParameterValueNameInput, ParameterItem>
{
    private readonly IParameterFactory parameterFactory;

    public ParameterValueNameTool(IParameterFactory parameterFactory)
    {
        this.parameterFactory = parameterFactory;
    }

    public override string Name => "parameter_value_name";
    public override string Description => "Resolves a numeric value of a parameter to its readable name (i.e. spell id -> spell name, flags -> flag names).";

    protected override Task<ParameterItem> Execute(ParameterValueNameInput input, CancellationToken token)
    {
        if (!parameterFactory.IsRegisteredLong(input.Key))
            throw new McpToolException($"Unknown parameter key: {input.Key} (see parameter_list)");

        var parameter = parameterFactory.Factory(input.Key);
        string? description = null;
        if (parameter.Items != null && parameter.Items.TryGetValue(input.Value, out var option))
            description = option.Description;

        return Task.FromResult(new ParameterItem
        {
            Value = input.Value,
            Name = parameter.ToString(input.Value),
            Description = description
        });
    }
}
