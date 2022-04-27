using System.Collections.Generic;
using System.Linq;
using WDE.Common.Database;

namespace WDE.QuestChainEditor.Models;

public class Quest
{
    public Quest(IQuestTemplate questTemplate)
    {
        Entry = questTemplate.Entry;
        Template = questTemplate;
    }   

    public uint Entry { get; }
    private List<QuestGroup> requirements = new();

    public void AddRequirement(QuestGroup entry)
    {
        if (entry.GroupId != 0)
        {
            var existing = requirements.FirstOrDefault(group => group.GroupId == entry.GroupId);
            if (existing != null)
            {
                existing.Merge(entry);
                return;
            }
        }
        requirements.Add(entry);
    }

    public IReadOnlyList<IQuestGroup> Requirements => requirements;
    public IQuestTemplate Template { get; }
}