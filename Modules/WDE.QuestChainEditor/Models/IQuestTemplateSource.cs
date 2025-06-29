using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;

namespace WDE.QuestChainEditor.Models;

public interface IQuestTemplateSource
{
    public Task<IQuestTemplate?> GetTemplate(uint entry);
    public Task<IEnumerable<IQuestTemplate>> GetByExclusiveGroup(int exclusiveGroup);
    public Task<IEnumerable<IQuestTemplate>> GetByPreviousQuestId(uint previous);
    public Task<IEnumerable<IQuestTemplate>> GetByNextQuestId(uint previous);
    public Task<IEnumerable<IQuestTemplate>> GetByBreadCrumbQuestId(uint questId);
    public Task<IReadOnlyList<(IQuestTemplate AllianceQuest, IQuestTemplate HordeQuest)>> GetQuestFactionChange(uint[] questIds);
}