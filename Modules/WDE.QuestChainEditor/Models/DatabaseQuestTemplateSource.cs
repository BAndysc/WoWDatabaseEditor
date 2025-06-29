using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WDE.QuestChainEditor.Models;

[AutoRegister]
public class DatabaseQuestTemplateSource : IQuestTemplateSource
{
    private readonly IDatabaseProvider databaseProvider;

    public DatabaseQuestTemplateSource(ICachedDatabaseProvider databaseProvider)
    {
        this.databaseProvider = databaseProvider;
    }
    
    public Task<IQuestTemplate?> GetTemplate(uint entry)
    {
        return databaseProvider.GetQuestTemplate(entry);
    }

    public async Task<IEnumerable<IQuestTemplate>> GetByExclusiveGroup(int exclusiveGroup)
    {
        // optimize this!
        return (await databaseProvider.GetQuestTemplatesAsync()).Where(qt => qt.ExclusiveGroup == exclusiveGroup);
    }

    public async Task<IEnumerable<IQuestTemplate>> GetByPreviousQuestId(uint previous)
    {
        // optimize this!
        return (await databaseProvider.GetQuestTemplatesAsync()).Where(qt => qt.PrevQuestId == previous || qt.PrevQuestId == -(int)previous);
    }

    public async Task<IEnumerable<IQuestTemplate>> GetByNextQuestId(uint previous)
    {
        // optimize this!
        return (await databaseProvider.GetQuestTemplatesAsync()).Where(qt => qt.NextQuestId == previous);
    }

    public async Task<IEnumerable<IQuestTemplate>> GetByBreadCrumbQuestId(uint questId)
    {
        // optimize this!
        return (await databaseProvider.GetQuestTemplatesAsync()).Where(qt => qt.BreadcrumbForQuestId == questId);
    }

    public async Task<IReadOnlyList<(IQuestTemplate AllianceQuest, IQuestTemplate HordeQuest)>> GetQuestFactionChange(uint[] questIds)
    {
        var factionChangeQuests = await databaseProvider.GetQuestFactionChanges();
        var byAlliance = factionChangeQuests
            .GroupBy(x => x.AllianceQuestId, x => x.HordeQuestId)
            .ToDictionary(x => x.Key, x => x.ToList());
        var byHorde = factionChangeQuests
            .GroupBy(x => x.HordeQuestId, x => x.AllianceQuestId)
            .ToDictionary(x => x.Key, x => x.ToList());

        var selectedQuestIds = new HashSet<(uint Alliance, uint Horde)>();
        var selectedQuests = new List<(IQuestTemplate AllianceQuest, IQuestTemplate HordeQuest)>();
        foreach (var questId in questIds)
        {
            if (byAlliance.TryGetValue(questId, out var hordeQuests))
            {
                foreach (var hordeQuest in hordeQuests)
                    selectedQuestIds.Add((questId, hordeQuest));
            }
            else if (byHorde.TryGetValue(questId, out var allianceQuests))
            {
                foreach (var allianceQuest in allianceQuests)
                    selectedQuestIds.Add((allianceQuest, questId));
            }
        }
        foreach (var (allianceQuestId, hordeQuestId) in selectedQuestIds)
        {
            var allianceQuest = await GetTemplate(allianceQuestId);
            var hordeQuest = await GetTemplate(hordeQuestId);
            if (allianceQuest == null)
            {
                LOG.LogInformation($"Quest {allianceQuestId} not found in database, but it was selected as a faction change quest.");
            }
            if (hordeQuest == null)
            {
                LOG.LogInformation($"Quest {hordeQuestId} not found in database, but it was selected as a faction change quest.");
            }
            if (allianceQuest != null && hordeQuest != null)
            {
                selectedQuests.Add((allianceQuest, hordeQuest));
            }
        }

        return selectedQuests;
    }
}