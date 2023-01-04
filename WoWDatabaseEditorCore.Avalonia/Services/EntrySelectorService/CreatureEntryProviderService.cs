using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using WDE.Common;
using WDE.Common.Avalonia.Controls;
using WDE.Common.Collections;
using WDE.Common.Database;
using WDE.Common.Database.Counters;
using WDE.Common.DBC;
using WDE.Common.TableData;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Avalonia.Services.EntrySelectorService;

[AutoRegister]
[SingleInstance]
public class CreatureEntryOrGuidProviderService : ICreatureEntryOrGuidProviderService
{
    private readonly ITabularDataPicker tabularDataPicker;
    private readonly IFactionTemplateStore factionTemplateStore;
    private readonly IDatabaseRowsCountProvider databaseRowsCountProvider;
    private readonly ICachedDatabaseProvider databaseProvider;

    public CreatureEntryOrGuidProviderService(ITabularDataPicker tabularDataPicker,
        IFactionTemplateStore factionTemplateStore,
        IDatabaseRowsCountProvider databaseRowsCountProvider,
        ICachedDatabaseProvider databaseProvider)
    {
        this.tabularDataPicker = tabularDataPicker;
        this.factionTemplateStore = factionTemplateStore;
        this.databaseRowsCountProvider = databaseRowsCountProvider;
        this.databaseProvider = databaseProvider;
    }
    
    private async Task<(ITabularDataArgs<ICreatureTemplate>, int index)> BuildTable(string? customCounterTable, uint? entry)
    {
        var index = -1;
        var creatures = await databaseProvider.GetCreatureTemplatesAsync();

        if (entry.HasValue)
        {
            for (int i = 0, count = creatures.Count; i < count; ++i)
                if (creatures[i].Entry == entry)
                {
                    index = i;
                    break;
                }
        }

        var columns = new List<ITabularDataColumn>()
        {
            new TabularDataColumn(nameof(ICreatureTemplate.Entry), "Entry", 60),
            new TabularDataColumn(nameof(ICreatureTemplate.FactionTemplate), "React", 50, new FuncDataTemplate(
                _ => true,
                (_, _) => new TeamFactionReactionView()
                {
                    [!TeamFactionReactionView.FactionTemplateIdProperty] =
                        new Binding(nameof(ICreatureTemplate.FactionTemplate))
                })),
            new TabularDataColumn(nameof(ICreatureTemplate.Name), "Name", 180),
            new TabularDataColumn(nameof(ICreatureTemplate.SubName), "Sub name", 110),
            new TabularDataAsyncColumn<ICreatureTemplate>(".", "Level", (template, token) =>
            {
                string level = template.MinLevel == template.MaxLevel
                    ? template.MinLevel.ToString()
                    : $"{template.MinLevel} - {template.MaxLevel}";
                return Task.FromResult<string?>(level);
            }, 50),
            new TabularDataAsyncColumn<uint>(nameof(ICreatureTemplate.FactionTemplate), "Faction", (factionTemplate, token)
                => Task.FromResult<string?>(factionTemplateStore.GetFactionByTemplate(factionTemplate)?.Name ??
                                            factionTemplate.ToString()), 90)
        };
        if (customCounterTable != null)
        {
            columns.Add(new TabularDataAsyncColumn<uint>(nameof(ICreatureTemplate.Entry), "Count", async (spellId, token) =>
            {
                var count = await databaseRowsCountProvider.GetRowsCountByPrimaryKey(DatabaseTable.WorldTable(customCounterTable), spellId, token);
                return count.ToString();
            }, 50));
        }
        else
        {
            columns.Add(new TabularDataAsyncColumn<uint>(nameof(ICreatureTemplate.Entry), "Spawns",
                async (creatureId, token) =>
                {
                    var count = await databaseRowsCountProvider.GetCreaturesCountByEntry(creatureId, token);
                    return count.ToString();
                }, 50));
        }

        var table = new TabularDataBuilder<ICreatureTemplate>()
            .SetTitle("Pick a creature")
            .SetData(creatures.AsIndexedCollection())
            .SetColumns(columns)
            .SetFilter((template, text) => template.Entry.Contains(text) ||
                                           template.Name.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                                           (template.SubName?.Contains(text, StringComparison.OrdinalIgnoreCase) ??
                                            false))
            .SetNumberPredicate((template, num) => template.Entry == num)
            .SetExactMatchPredicate((template, search) => template.Entry.Is(search))
            .SetExactMatchCreator(search =>
            {
                if (!int.TryParse(search, out var entry))
                    return null;
                if (entry >= 0)
                {
                    return new AbstractCreatureTemplate()
                    {
                        Entry = (uint)entry,
                        Name = "Pick non existing"
                    };
                }
                else
                {
                    var creature = databaseProvider.GetCachedCreatureByGuid(0, (uint)(-entry));
                    var template = creature == null ? null : databaseProvider.GetCachedCreatureTemplate(creature.Entry);
                    if (template != null)
                    {
                        return new ExtendedAbstractCreatureTemplate()
                        {
                            Guid = entry,
                            Entry = template.Entry,
                            FactionTemplate = template.FactionTemplate,
                            Name = "Guid " + (-entry) + " :: " + template.Name,
                            SubName = template.SubName,
                            MinLevel = template.MinLevel,
                            MaxLevel = template.MaxLevel
                        };
                    }
                    else
                    {
                        return new ExtendedAbstractCreatureTemplate()
                        {
                            Entry = 0,
                            Guid = entry,
                            Name = "Non existing guid " + (-entry)
                        };
                    }
                }
            })
            .Build();
        return (table, index);
    }

    public async Task<int?> GetEntryFromService(uint? entry, string? customCounterTable = null)
    {
        var (table, index) = await BuildTable(customCounterTable, entry);

        var result = await tabularDataPicker.PickRow(table, index, entry.HasValue && entry > 0 ? entry.ToString() : null);
        
        return result == null ? null : ExtractGuidOrEntry(result);
    }

    public async Task<IReadOnlyCollection<int>> GetEntriesFromService(string? customCounterTable = null)
    {
        var (table, index) = await BuildTable(customCounterTable, null);

        var result = await tabularDataPicker.PickRows(table);
        
        return result == null ? Array.Empty<int>() : result.Select(ExtractGuidOrEntry).ToList();
    }

    private int ExtractGuidOrEntry(ICreatureTemplate creatureTemplate)
    {
        if (creatureTemplate is ExtendedAbstractCreatureTemplate extended)
            return extended.Guid;
        return (int)creatureTemplate.Entry;
    }
    
    private class ExtendedAbstractCreatureTemplate : AbstractCreatureTemplate
    {
        public int Guid { get; set; }
    }
}