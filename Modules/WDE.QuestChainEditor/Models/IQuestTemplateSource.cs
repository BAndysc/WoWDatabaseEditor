using System.Collections.Generic;
using WDE.Common.Database;

namespace WDE.QuestChainEditor.Models;

public interface IQuestTemplateSource
{
    public IQuestTemplate? GetTemplate(uint entry);
    public IEnumerable<IQuestTemplate> GetByPreviousQuestId(uint previous);
    public IEnumerable<IQuestTemplate> GetByNextQuestId(uint previous);
}