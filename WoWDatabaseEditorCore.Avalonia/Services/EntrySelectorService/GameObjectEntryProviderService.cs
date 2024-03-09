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
public class GameObjectEntryProviderService : IGameobjectEntryOrGuidProviderService
{
    private readonly ITabularDataPicker tabularDataPicker;
    private readonly IFactionTemplateStore factionTemplateStore;
    private readonly IDatabaseRowsCountProvider databaseRowsCountProvider;
    private readonly ICachedDatabaseProvider databaseProvider;

    public GameObjectEntryProviderService(ITabularDataPicker tabularDataPicker,
        IFactionTemplateStore factionTemplateStore,
        IDatabaseRowsCountProvider databaseRowsCountProvider,
        ICachedDatabaseProvider databaseProvider)
    {
        this.tabularDataPicker = tabularDataPicker;
        this.factionTemplateStore = factionTemplateStore;
        this.databaseRowsCountProvider = databaseRowsCountProvider;
        this.databaseProvider = databaseProvider;
    }
    
    private async Task<(ITabularDataArgs<IGameObjectTemplate>, int index)> BuildTable(string? customCounterTable, uint? entry)
    {
        var index = -1;
        var gameObjects = await databaseProvider.GetGameObjectTemplatesAsync();

        if (entry.HasValue)
        {
            for (int i = 0, count = gameObjects.Count; i < count; ++i)
                if (gameObjects[i].Entry == entry)
                {
                    index = i;
                    break;
                }
        }

        var columns = new List<ITabularDataColumn>()
        {
            new TabularDataColumn(nameof(IGameObjectTemplate.Entry), "Entry", 80),
            new TabularDataColumn(nameof(IGameObjectTemplate.Name), "Name", 210),
            new TabularDataColumn(nameof(IGameObjectTemplate.Type), "Type", 120)
        };
        if (customCounterTable != null)
        {
            columns.Add(new TabularDataAsyncColumn<uint>(nameof(IGameObjectTemplate.Entry), "Count", async (entry, token) =>
            {
                var count = await databaseRowsCountProvider.GetRowsCountByPrimaryKey(DatabaseTable.WorldTable(customCounterTable), entry, token);
                return count.ToString();
            }, 50));
        }
        else
        {
            columns.Add(new TabularDataAsyncColumn<uint>(nameof(IGameObjectTemplate.Entry), "Spawns",
                async (entry, token) =>
                {
                    var count = await databaseRowsCountProvider.GetGameObjectCountByEntry(entry, token);
                    return count.ToString();
                }, 50));
        }

        var table = new TabularDataBuilder<IGameObjectTemplate>()
            .SetTitle("Pick a gameobject")
            .SetData(gameObjects.AsIndexedCollection())
            .SetColumns(columns)
            .SetFilter((template, text) => template.Entry.Contains(text) ||
                                           template.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
            .SetNumberPredicate((template, num) => template.Entry == num)
            .SetExactMatchPredicate((template, search) => template.Entry.Is(search))
            .SetExactMatchCreator(search =>
            {
                if (!int.TryParse(search, out var entry))
                    return null;
                if (entry >= 0)
                {
                    return new AbstractGameObjectTemplate()
                    {
                        Entry = (uint)entry,
                        Name = "Pick non existing"
                    };
                }
                else
                {
                    var creature = databaseProvider.GetCachedGameObjectByGuid(0, (uint)(-entry));
                    var template = creature == null ? null : databaseProvider.GetCachedGameObjectTemplate(creature.Entry);
                    if (template != null)
                    {
                        return new ExtendedAbstractGameObjectTemplate()
                        {
                            Guid = entry,
                            Entry = template.Entry,
                            Name = "Guid " + (-entry) + " :: " + template.Name,
                            Type = template.Type
                        };
                    }
                    else
                    {
                        return new ExtendedAbstractGameObjectTemplate()
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
        
        return (int?)result?.Entry;
    }

    public async Task<IReadOnlyCollection<int>> GetEntriesFromService(string? customCounterTable = null)
    {
        var (table, index) = await BuildTable(customCounterTable, null);

        var result = await tabularDataPicker.PickRows(table);
        
        return result == null ? Array.Empty<int>() : result.Select(x => (int)x.Entry).ToList();
    }

    private int ExtractGuidOrEntry(IGameObjectTemplate gameobjectTemplate)
    {
        if (gameobjectTemplate is ExtendedAbstractGameObjectTemplate extended)
            return extended.Guid;
        return (int)gameobjectTemplate.Entry;
    }
    
    private class ExtendedAbstractGameObjectTemplate : AbstractGameObjectTemplate
    {
        public int Guid { get; set; }
    }
}