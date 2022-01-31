using System;
using System.Collections.Generic;
using System.Linq;
using WDE.Module.Attributes;
using WDE.QuestChainEditor.Models;

namespace WDE.QuestChainEditor.QueryGenerators;

[SingleInstance]
[AutoRegister]
public class QueryGenerator : IQueryGenerator
{
    public IEnumerable<ChainRawData> Generate(QuestStore questStore)
    {
        Dictionary<uint, ChainRawData> datas = new();
        foreach (var q in questStore)
            datas[q.Entry] = new(q.Entry);

        foreach (var q in questStore)
        {
            if (q.Requirements.Count == 1)
            {
                var group = q.Requirements[0];
                if (group.Quests.Count == 1)
                {
                    var requirement = group.Quests[0];
                    if (group.RequirementType == QuestRequirementType.MustBeActive)
                        datas[q.Entry].PrevQuestId = -(int)requirement;
                    else
                        datas[q.Entry].PrevQuestId = (int)requirement;
                }
                else if (group.Quests.Count > 0)
                {
                    int exclusiveGroup = (int)(group.Quests.Min());
                    if (group.RequirementType == QuestRequirementType.MustBeActive)
                        throw new Exception("Cannot handle exclusive group with multiple quests");
                    else if (group.RequirementType == QuestRequirementType.AllCompleted)
                        exclusiveGroup = -exclusiveGroup;
                    else if (group.RequirementType == QuestRequirementType.OnlyOneCompleted)
                        exclusiveGroup = exclusiveGroup;

                    foreach (var requirement in group.Quests)
                    {
                        datas[requirement].ExclusiveGroup = exclusiveGroup;
                        datas[requirement].NextQuestId = (int)q.Entry;
                    }
                }
            }
            else if (q.Requirements.Count > 0)
            {
                foreach (var group in q.Requirements)
                {
                    if (group.Quests.Count == 1)
                    {
                        var requirement = group.Quests[0];
                        if (group.RequirementType == QuestRequirementType.MustBeActive)
                            throw new Exception("Cannot handle single quest with multiple quests as active requirement");
                        else
                            datas[requirement].NextQuestId = (int)q.Entry;
                    }
                    else if (group.Quests.Count > 0)
                    {
                        int exclusiveGroup = (int)(group.Quests.Min());
                        if (group.RequirementType == QuestRequirementType.MustBeActive)
                            throw new Exception("Cannot handle exclusive group with multiple quests");
                        else if (group.RequirementType == QuestRequirementType.AllCompleted)
                            exclusiveGroup = -exclusiveGroup;
                        else if (group.RequirementType == QuestRequirementType.OnlyOneCompleted)
                            exclusiveGroup = exclusiveGroup;

                        foreach (var requirement in group.Quests)
                        {
                            datas[requirement].ExclusiveGroup = exclusiveGroup;
                            datas[requirement].NextQuestId = (int)q.Entry;
                        }
                    }
                }
            }
        }

        return datas.Values;
    }
}