using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using WDE.Common;
using WDE.Common.Avalonia.Controls;
using WDE.Common.Collections;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.TableData;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Avalonia.Services.EntrySelectorService;

[AutoRegister]
public class QuestEntryProviderService : IQuestEntryProviderService
{
    private readonly ITabularDataPicker tabularDataPicker;
    private readonly IDatabaseProvider database;
    private readonly IParameterFactory parameterFactory;

    public QuestEntryProviderService(ITabularDataPicker tabularDataPicker,
        IDatabaseProvider database,
        IParameterFactory parameterFactory)
    {
        this.tabularDataPicker = tabularDataPicker;
        this.database = database;
        this.parameterFactory = parameterFactory;
    }

    private async Task<(ITabularDataArgs<IQuestTemplate>, int index)> BuildTable(uint? questId)
    {
        var questSortParameter = parameterFactory.Factory("ZoneOrQuestSortParameter");
        var questSortConverter = new FuncValueConverter<int, string>(id =>
        {
            if (id == 0)
                return "";
            if (questSortParameter.Items?.TryGetValue(id, out var item) == true)
                return item.Name;
            return id.ToString();
        });
        
        var templates = await database.GetQuestTemplatesAsync();
        var index = questId.HasValue ? templates.IndexIf(t => t.Entry == questId) : -1;
        var table = new TabularDataBuilder<IQuestTemplate>()
            .SetTitle("Pick a quest")
            .SetData(templates.AsIndexedCollection())
            .SetColumns(new TabularDataColumn(nameof(IQuestTemplate.Entry), "Entry", 60),
                new TabularDataColumn(nameof(IQuestTemplate.AllowableRaces), "Team", 40, new FuncDataTemplate(_ => true,
                    (_, _) => new GameTeamImage()
                    {
                        [!GameTeamImage.RacesProperty] = new Binding(nameof(IQuestTemplate.AllowableRaces)),
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(2),
                        Spacing = 2,
                        Mode = GameTeamImageMode.MustContainAny,
                        IgnoreIfBoth = true
                    })),
                new TabularDataColumn(nameof(IQuestTemplate.MinLevel), "Min level", 36, new FuncDataTemplate(_ => true,
                    (_, _) => new TextBlock()
                    {
                        [!TextBlock.TextProperty] = new Binding(nameof(IQuestTemplate.MinLevel))
                            { StringFormat = "[{0}]" },
                        VerticalAlignment = VerticalAlignment.Center
                    })),
                new TabularDataColumn(nameof(IQuestTemplate.Name), "Title", 200),
                new TabularDataColumn(nameof(IQuestTemplate.AllowableClasses), "Classes", 110, new FuncDataTemplate(
                    _ => true,
                    (_, _) => new GameClassesImage()
                    {
                        [!GameClassesImage.GameClassesProperty] = new Binding(nameof(IQuestTemplate.AllowableClasses)),
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(2),
                        Spacing = 2
                    })),
                new TabularDataColumn(nameof(IQuestTemplate.AllowableRaces), "Races", 150, new FuncDataTemplate(
                    _ => true,
                    (_, _) => new GameRacesImage()
                    {
                        [!GameRacesImage.RacesProperty] = new Binding(nameof(IQuestTemplate.AllowableRaces)),
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(2),
                        Spacing = 2,
                        IgnoreIfAllPerTeam = true
                    })),
                new TabularDataColumn(nameof(IQuestTemplate.QuestSortId), "Zone", 150, new FuncDataTemplate(_ => true,
                    (_, _) => new TextBlock()
                    {
                        [!TextBlock.TextProperty] = new Binding(nameof(IQuestTemplate.QuestSortId))
                            { Converter = questSortConverter },
                        VerticalAlignment = VerticalAlignment.Center
                    })))
            .SetFilter((template, filter) =>
            {
                if (template.Entry.Contains(filter))
                    return true;
                if (template.Name?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false)
                    return true;
                return false;
            })
            .SetNumberPredicate((template, num) => template.Entry == num)
            .SetExactMatchPredicate((template, search) => template.Entry.Is(search))
            .SetExactMatchCreator(search =>
            {
                if (!uint.TryParse(search, out var entry))
                    return null;
                return new AbstractQuestTemplate()
                {
                    Entry = entry,
                    Name = "Pick non existing"
                };
            })
            .Build();
        return (table, index);
    }

    public async Task<uint?> GetEntryFromService(uint? questId = null)
    {
        var (table, index) = await BuildTable(questId);
        var result = await tabularDataPicker.PickRow(table, index, questId.HasValue && questId > 0 ? questId.ToString() : null);
        return result?.Entry;
    }

    public async Task<IReadOnlyCollection<uint>> GetEntriesFromService()
    {
        var (table, index) = await BuildTable(null);
        var templates = await tabularDataPicker.PickRows(table);
        return templates == null ? Array.Empty<uint>() : templates.Select(t => t.Entry).ToList();
    }
}