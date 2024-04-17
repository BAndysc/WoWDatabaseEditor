using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using PropertyChanged.SourceGenerator;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.QuestChainEditor.Models;

namespace WDE.QuestChainEditor.ViewModels;

public partial class ExclusiveGroupViewModel : BaseQuestViewModel
{
    [Notify] [AlsoNotify(nameof(GroupTypeString), nameof(IsAnyGroupType), nameof(IsAllGroupType))]
    private QuestGroupType groupType;

    public bool IsAnyGroupType => groupType == QuestGroupType.OneOf;

    public bool IsAllGroupType => groupType == QuestGroupType.All;

    private Size size;
    public Size Size
    {
        get => size;
        set
        {
            if (size == value)
                return;
            size = value;
            RaisePropertyChanged();
        }
    }

    public void OnGroupTypeChanged()
    {
        Header = GroupTypeString;
    }

    public ObservableCollection<QuestViewModel> Quests { get; } = new();

    public string GroupTypeString => groupType switch
    {
        QuestGroupType.All => "ALL",
        QuestGroupType.OneOf => "ANY",
        _ => throw new ArgumentOutOfRangeException()
    };

    public ExclusiveGroupViewModel(QuestChainDocumentViewModel document, QuestGroupType groupType) : base(document)
    {
        this.groupType = groupType;
        Header = GroupTypeString;
        this.ToObservable(x => x.Location).SubscribeAction(newLoc =>
        {
            Arrange(default);
        });

        Quests.CollectionChanged += (sender, args) =>
        {
            if (args.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (QuestViewModel quest in args.NewItems!)
                {
                    if (quest.ExclusiveGroup != null)
                    {
                        quest.ExclusiveGroup.Quests.Remove(quest);
                    }
                    quest.ExclusiveGroup = this;
                }
            }
            else if (args.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (QuestViewModel quest in args.OldItems!)
                {
                    if (quest.ExclusiveGroup == this)
                        quest.ExclusiveGroup = null;
                }
            }
            Arrange(default);
        };
    }

    public void Arrange(Point offset)
    {
        var location = Location + offset;
        double width = 0;
        double height = 0;
        location += new Point(5, 15) + new Point(10, 10) /* state node margin */; // margin
        foreach (var quest in Quests)
        {
            var questWidth = Math.Max(50, quest.Bounds.Width);
            var questHeight = Math.Max(50, quest.Bounds.Height);
            quest.Location = location;
            location += new Point(questWidth + 5, 0);
            width += questWidth + 5;
            height = Math.Max(height, questHeight);
        }
        Size = new Size(width, height);
    }

    public bool AddQuest(QuestViewModel quest)
    {
        if (Quests.Contains(quest))
            return false;
        Quests.Add(quest);
        return true;
    }

    public void RemoveQuest(QuestViewModel quest)
    {
        Quests.Remove(quest);
    }

    public override string ToString()
    {
        if (Quests.Count == 0)
            return $"Empty {GroupType} Group";

        return $"{GroupType} Group ({EntryOrExclusiveGroupId})";
    }

    public override int EntryOrExclusiveGroupId => Quests.Count == 0 ? 0 : (int)Quests.Select(x => x.Entry).Min() * (groupType == QuestGroupType.All ? -1 : 1);
}