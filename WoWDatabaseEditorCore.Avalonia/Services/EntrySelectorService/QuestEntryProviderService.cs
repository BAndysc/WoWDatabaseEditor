using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using WDE.Common;
using WDE.Common.Avalonia.Controls;
using WDE.Common.Collections;
using WDE.Common.Database;
using WDE.Common.TableData;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Avalonia.Services.EntrySelectorService;

[AutoRegister]
public class QuestEntryProviderService : IQuestEntryProviderService
{
    private readonly ITabularDataPicker tabularDataPicker;
    private readonly IDatabaseProvider database;

    public QuestEntryProviderService(ITabularDataPicker tabularDataPicker, IDatabaseProvider database)
    {
        this.tabularDataPicker = tabularDataPicker;
        this.database = database;
    }

    public async Task<uint?> GetEntryFromService(uint? questId = null)
    {
        var templates = database.GetQuestTemplates();
        var index = questId.HasValue ? templates.IndexIf(t => t.Entry == questId) : -1;
        var result = await tabularDataPicker.PickRow(new TabularDataBuilder<IQuestTemplate>()
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
                new TabularDataColumn(nameof(IQuestTemplate.Name), "Title", 200),
                new TabularDataColumn(nameof(IQuestTemplate.AllowableClasses), "Classes", 110, new FuncDataTemplate(_ => true,
                    (_, _) => new GameClassesImage()
                    {
                        [!GameClassesImage.GameClassesProperty] = new Binding(nameof(IQuestTemplate.AllowableClasses)),
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(2),
                        Spacing = 2
                    })),
                new TabularDataColumn(nameof(IQuestTemplate.AllowableRaces), "Races", 150, new FuncDataTemplate(_ => true,
                    (_, _) => new GameRacesImage()
                    {
                        [!GameRacesImage.RacesProperty] = new Binding(nameof(IQuestTemplate.AllowableRaces)),
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(2),
                        Spacing = 2,
                        IgnoreIfAllPerTeam = true
                    })))
            .Build(), index);
        return result?.Entry;
    }
}