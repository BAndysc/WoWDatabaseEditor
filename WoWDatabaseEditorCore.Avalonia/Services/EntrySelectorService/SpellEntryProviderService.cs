using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
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
public class SpellEntryProviderService : ISpellEntryProviderService
{
    private readonly ITabularDataPicker tabularDataPicker;
    private readonly IDatabaseRowsCountProvider databaseRowsCountProvider;
    private readonly ISpellStore spellStore;

    public SpellEntryProviderService(ITabularDataPicker tabularDataPicker,
        IDatabaseRowsCountProvider databaseRowsCountProvider,
        ISpellStore spellStore)
    {
        this.tabularDataPicker = tabularDataPicker;
        this.databaseRowsCountProvider = databaseRowsCountProvider;
        this.spellStore = spellStore;
    }

    private ITabularDataArgs<ISpellEntry> BuildTable(string? customCounterTable, uint? spellId, out int index)
    {
        index = -1;
        var spells = spellStore.Spells;
        
        if (spellId.HasValue)
        {
            for (int i = 0, count = spells.Count; i < count; ++i)
                if (spells[i].Id == spellId)
                {
                    index = i;
                    break;
                }
        }

        var columns = new List<ITabularDataColumn>()
        {
            new TabularDataColumn(nameof(ISpellEntry.Id), "Entry", 60),
            new TabularDataColumn(nameof(ISpellEntry.Id), "Icon", 40, new FuncDataTemplate(_ => true,
                (_, _) => new SpellImage()
                {
                    [!SpellImage.SpellIdProperty] = new Binding(nameof(ISpellEntry.Id)),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(2)
                })),
            new TabularDataColumn(nameof(ISpellEntry.Name), "Name", 200),
            new TabularDataColumn(nameof(ISpellEntry.Aura), "Aura", 120),
            new TabularDataColumn(nameof(ISpellEntry.Targets), "Targets", 140)
        };
        if (customCounterTable != null)
        {
            columns.Add(new TabularDataAsyncColumn<uint>(nameof(ISpellEntry.Id), "Count", async (spellId, token) =>
            {
                var count = await databaseRowsCountProvider.GetRowsCountByPrimaryKey(DatabaseTable.WorldTable(customCounterTable), spellId, token);
                return count.ToString();
            }, 50));
        }

        return new TabularDataBuilder<ISpellEntry>()
            .SetTitle("Pick a spell")
            .SetData(spells.AsIndexedCollection())
            .SetColumns(columns)
            .SetFilter((entry, text) =>
                entry.Id.Contains(text) || entry.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
            .SetExactMatchPredicate((entry, text) => entry.Id.Is(text))
            .SetNumberPredicate((template, num) => template.Id == num)
            .SetExactMatchCreator((text) =>
            {
                if (uint.TryParse(text, out var entry))
                    return new AbstractSpellEntry() { Id = entry, Name = "Non existing spell" };
                return null;
            })
            .Build();
    }

    public async Task<uint?> GetEntryFromService(uint? spellId = null, string? customCounterTable = null)
    {
        var table = BuildTable(customCounterTable, spellId, out var index);
        var result = await tabularDataPicker.PickRow(table, index, spellId.HasValue && spellId > 0 ? spellId.ToString() : null);
        return result?.Id;
    }

    public async Task<IReadOnlyCollection<uint>> GetEntriesFromService(string? customCounterTable = null)
    {
        var table = BuildTable(customCounterTable, null, out _);
        var spells = await tabularDataPicker.PickRows(table);
        return spells == null ? Array.Empty<uint>() : spells.Select(x => x.Id).ToList();
    }
}