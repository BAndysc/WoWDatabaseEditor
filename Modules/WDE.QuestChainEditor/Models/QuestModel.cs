using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WDE.Common.Database;

namespace WDE.QuestChainEditor.Models;

public class QuestModel
{
    public QuestModel(IQuestTemplate questTemplate)
    {
        Entry = questTemplate.Entry;
        AllowableClasses = questTemplate.AllowableClasses;
        AllowableRaces = questTemplate.AllowableRaces;
        Template = questTemplate;
    }   

    public uint Entry { get; }
    public CharacterClasses AllowableClasses { get; set; }
    public CharacterRaces AllowableRaces { get; set; }
    public OtherFactionQuest? OtherFactionQuest { get; set; }
    public IReadOnlyList<ICondition> Conditions { get; set; } = Array.Empty<ICondition>();
    private List<QuestGroup> mustBeCompleted = new();
    private List<QuestGroup> mustBeActive = new();
    private List<QuestGroup> breadcrumbs = new();

    public void AddRequirement(QuestRequirementType type, QuestGroup entry)
    {
        List<QuestGroup> collection;
        switch (type)
        {
            case QuestRequirementType.Completed:
                collection = mustBeCompleted;
                break;
            case QuestRequirementType.MustBeActive:
                collection = mustBeActive;
                break;
            case QuestRequirementType.Breadcrumb:
                collection = breadcrumbs;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        if (entry.GroupId != 0)
        {
            var existing = collection.FirstOrDefault(group => group.GroupId == entry.GroupId);
            if (existing != null)
            {
                existing.Merge(entry);
                return;
            }
        }

        var indexOfEntry = collection.BinarySearch(entry, Comparer<QuestGroup>.Create((a, b) => a.Quests[0].CompareTo(b.Quests[0])));
        if (indexOfEntry < 0)
            collection.Insert(~indexOfEntry, entry);
        else
            collection.Add(entry);
    }

    public IReadOnlyList<IQuestGroup> MustBeCompleted => mustBeCompleted;
    public IReadOnlyList<IQuestGroup> MustBeActive => mustBeActive;
    public IReadOnlyList<IQuestGroup> Breadcrumbs => breadcrumbs;
    public IQuestTemplate Template { get; }

    public override string ToString()
    {
        if (mustBeCompleted.Count == 0 && mustBeActive.Count == 0 && breadcrumbs.Count == 0)
            return $"Quest {Entry} has no requirements";
        StringBuilder sb = new();
        sb.Append($"Quest {Entry}:");
        if (mustBeCompleted.Count + mustBeActive.Count > 0)
        {
            sb.Append($"you need to ");
            if (mustBeCompleted.Count > 0)
            {
                sb.Append("complete (");
                sb.Append(string.Join(" OR ", mustBeCompleted.Select(group => group.ToString())));
                sb.Append(") ");
                if (mustBeActive.Count > 0)
                    sb.Append("and ");
            }
            if (mustBeActive.Count > 0)
            {
                sb.Append("have active (");
                sb.Append(string.Join(" OR ", mustBeActive.Select(group => group.ToString())));
                sb.Append(") ");
            }

            if (breadcrumbs.Count > 0)
                sb.Append("; ");
        }

        if (breadcrumbs.Count > 0)
        {
            sb.Append("you may complete (" + string.Join(" OR ", breadcrumbs.Select(group => group.ToString())) + ")");
        }

        return sb.ToString();
    }

    public QuestModel Clone()
    {
        var clone = new QuestModel(Template);
        clone.mustBeCompleted = mustBeCompleted.Select(x => x.Clone()).ToList();
        clone.mustBeActive = mustBeActive.Select(x => x.Clone()).ToList();
        clone.breadcrumbs = breadcrumbs.Select(x => x.Clone()).ToList();
        clone.AllowableClasses = AllowableClasses;
        clone.AllowableRaces = AllowableRaces;
        clone.OtherFactionQuest = OtherFactionQuest;
        clone.Conditions = Conditions.Select(x => new AbstractCondition(x)).ToList();
        return clone;
    }
}