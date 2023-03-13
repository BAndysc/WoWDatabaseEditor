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
    private readonly IDatabaseProvider databaseProvider;

    public CreatureEntryOrGuidProviderService(ITabularDataPicker tabularDataPicker,
        IFactionTemplateStore factionTemplateStore,
        IDatabaseProvider databaseProvider)
    {
        this.tabularDataPicker = tabularDataPicker;
        this.factionTemplateStore = factionTemplateStore;
        this.databaseProvider = databaseProvider;
    }
    
    public async Task<int?> GetEntryFromService(uint? entry)
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
        var result = await tabularDataPicker.PickRow(new TabularDataBuilder<ICreatureTemplate>()
            .SetTitle("Pick a creature")
            .SetData(creatures.AsIndexedCollection())
            .SetColumns(new TabularDataColumn(nameof(ICreatureTemplate.Entry), "Entry", 60),
                new TabularDataColumn(nameof(ICreatureTemplate.FactionTemplate), "React", 50, new FuncDataTemplate(_ => true,
                    (_, _) => new TeamFactionReactionView()
                    {
                        [!TeamFactionReactionView.FactionTemplateIdProperty] = new Binding(nameof(ICreatureTemplate.FactionTemplate))
                    })),
                new TabularDataColumn(nameof(ICreatureTemplate.Name), "Name", 220),
                new TabularDataColumn(nameof(ICreatureTemplate.SubName), "Sub name", 120),
                new TabularDataAsyncColumn<ICreatureTemplate>(".", "Level", (template, token) =>
                {
                    string level = template.MinLevel == template.MaxLevel ? template.MinLevel.ToString() : $"{template.MinLevel} - {template.MaxLevel}";
                    return Task.FromResult<string?>(level);
                }, 60),
                new TabularDataAsyncColumn<uint>(nameof(ICreatureTemplate.FactionTemplate), "Faction", (factionTemplate, token) 
                    => Task.FromResult<string?>(factionTemplateStore.GetFactionByTemplate(factionTemplate)?.Name ?? factionTemplate.ToString()), 120))
            .SetFilter((template, text) => template.Entry.Contains(text) || 
                                        template.Name.Contains(text, StringComparison.OrdinalIgnoreCase) || 
                                        (template.SubName?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false))
            .Build(), index);
        
        return (int?)result?.Entry;
    }
}