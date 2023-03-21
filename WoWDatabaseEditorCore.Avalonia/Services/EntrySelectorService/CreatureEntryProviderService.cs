using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using WDE.Common;
using WDE.Common.Avalonia.Controls;
using WDE.Common.Collections;
using WDE.Common.Database;
using WDE.Common.Database.Counters;
using WDE.Common.DBC;
using WDE.Common.Parameters;
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
    private readonly IDatabaseProvider databaseProvider;

    public CreatureEntryOrGuidProviderService(ITabularDataPicker tabularDataPicker,
        IFactionTemplateStore factionTemplateStore,
        IDatabaseRowsCountProvider databaseRowsCountProvider,
        IDatabaseProvider databaseProvider)
    {
        this.tabularDataPicker = tabularDataPicker;
        this.factionTemplateStore = factionTemplateStore;
        this.databaseRowsCountProvider = databaseRowsCountProvider;
        this.databaseProvider = databaseProvider;
    }
    
    public async Task<int?> GetEntryFromService(uint? entry, string? customCounterTable = null)
    {
        var index = -1;
        var creatures = databaseProvider.GetCreatureTemplates();
        
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
                string level = template.MinLevel == template.MaxLevel ? template.MinLevel.ToString() : $"{template.MinLevel} - {template.MaxLevel}";
                return Task.FromResult<string?>(level);
            }, 50),
            new TabularDataAsyncColumn<uint>(nameof(ICreatureTemplate.FactionTemplate), "Faction", (factionTemplate, token)
                    => Task.FromResult<string?>(factionTemplateStore.GetFactionByTemplate(factionTemplate)?.Name ?? factionTemplate.ToString()), 90)
        };
        if (customCounterTable != null)
        {
            columns.Add(new TabularDataAsyncColumn<uint>(nameof(ICreatureTemplate.Entry), "Count", async (spellId, token) =>
            {
                var count = await databaseRowsCountProvider.GetRowsCountByPrimaryKey(customCounterTable, spellId, token);
                return count.ToString();
            }, 50));
        }
        else
        {
            columns.Add(new TabularDataAsyncColumn<uint>(nameof(ICreatureTemplate.Entry), "Spawns", async (creatureId, token) =>
            {
                var count = await databaseRowsCountProvider.GetCreaturesCountByEntry(creatureId, token);
                return count.ToString();
            }, 50));
        }
        
        var result = await tabularDataPicker.PickRow(new TabularDataBuilder<ICreatureTemplate>()
            .SetTitle("Pick a creature")
            .SetData(creatures.AsIndexedCollection())
            .SetColumns(columns)
            .SetFilter((template, text) => template.Entry.Contains(text) || 
                                        template.Name.Contains(text, StringComparison.OrdinalIgnoreCase) || 
                                        (template.SubName?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false))
            .Build(), index);
        
        return (int?)result?.Entry;
    }
}