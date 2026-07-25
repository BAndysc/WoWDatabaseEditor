using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Services.FindAnywhere;
using WDE.Common.Services.Mcp;
using WDE.Common.Solution;
using WDE.Module.Attributes;

namespace WDE.Mcp.Tools.FindAnywhere;

public sealed class FindAnywhereInput
{
    [Description("Parameter names to search usages of, i.e. ['SpellParameter'] or ['CreatureParameter', 'CreatureGameobjectParameter']")]
    public required List<string> ParameterNames { get; init; }

    [Description("Values to find (i.e. a spell id or creature entry)")]
    public required List<long> Values { get; init; }

    [Description("Optional source filter; any of: SmartScripts, Tables, Spawns, Conditions, Sniffs, Dbc, EventAi, Other, SourceCode, Waypoints. Default: all")]
    public List<string>? SourceTypes { get; init; }

    [Description("Maximum number of results (default 200)")]
    public int Limit { get; init; } = 200;
}

public sealed class FindAnywhereResultItem
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public long? Entry { get; init; }
    public string? SolutionItem { get; init; }
}

[AutoRegister]
[SingleInstance]
public class FindAnywhereTool : McpTool<FindAnywhereInput, List<FindAnywhereResultItem>>
{
    private readonly IFindAnywhereService findAnywhereService;
    private readonly ISolutionItemNameRegistry nameRegistry;

    public FindAnywhereTool(IFindAnywhereService findAnywhereService, ISolutionItemNameRegistry nameRegistry)
    {
        this.findAnywhereService = findAnywhereService;
        this.nameRegistry = nameRegistry;
    }

    public override string Name => "find_anywhere";
    public override string Description => "The editor's 'find anywhere': finds all usages of given values (spells, creatures, quests, ...) across scripts, database tables, spawns, conditions and more. Parameter names scope the search (see parameter_list).";

    protected override async Task<List<FindAnywhereResultItem>> Execute(FindAnywhereInput input, CancellationToken token)
    {
        if (input.ParameterNames.Count == 0 || input.Values.Count == 0)
            throw new McpToolException("parameterNames and values must be non-empty");

        var sourceTypes = FindAnywhereSourceType.All;
        if (input.SourceTypes is { Count: > 0 })
        {
            sourceTypes = FindAnywhereSourceType.None;
            foreach (var sourceName in input.SourceTypes)
            {
                if (!Enum.TryParse<FindAnywhereSourceType>(sourceName, true, out var parsed))
                    throw new McpToolException($"Unknown source type: {sourceName}");
                sourceTypes |= parsed;
            }
        }

        var context = new ToListFindAnywhereResultContext();
        await findAnywhereService.Find(context, sourceTypes, input.ParameterNames, input.Values, token);

        return context.Results
            .Take(Math.Max(1, input.Limit))
            .Select(r => new FindAnywhereResultItem
            {
                Title = r.Title,
                Description = r.Description,
                Entry = r.Entry,
                SolutionItem = r.SolutionItem == null ? null : TryGetName(r.SolutionItem)
            })
            .ToList();
    }

    private string? TryGetName(ISolutionItem item)
    {
        try
        {
            return nameRegistry.GetName(item);
        }
        catch (Exception)
        {
            return item.GetType().Name;
        }
    }
}
