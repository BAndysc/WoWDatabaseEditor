using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Microsoft.Extensions.Logging;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.QuestChainEditor.Models;

namespace WDE.QuestChainEditor.ViewModels;

public partial class QuestChainDocumentViewModel
{
    public void DeleteSelectedConnections()
    {
        var connectedNodes = SelectedConnections
            .Select(conn => (conn, conn.RequirementType, conn.FromNode, conn.ToNode))
            .Where(x => x.FromNode != null && x.ToNode != null)
            .Select(x => (x.conn, x.RequirementType, x.FromNode!, x.ToNode!))
            .ToList();

        if (connectedNodes.Count == 0)
            return;

        historyHandler.DoAction(new AnonymousHistoryAction("Remove connections", () =>
        {
            foreach (var (conn, type, from, to) in connectedNodes)
            {
                conn.Attach(from.Connector, to.Connector);
                Connections.Add(conn);
            }
        }, () =>
        {
            foreach (var (conn, type, from, to) in connectedNodes)
            {
                conn.Detach();
                Connections.Remove(conn);
            }
        }));
    }

    public void DegroupSelected()
    {
        List<(QuestViewModel, ExclusiveGroupViewModel)>? questGroups = null;
        foreach (var selected in SelectedItems)
        {
            if (selected is QuestViewModel quest)
            {
                if (quest.ExclusiveGroup != null)
                {
                    // grabbing an exclusive group, ignore inner quests
                    if (!SelectedItems.Contains(quest.ExclusiveGroup))
                    {
                        questGroups ??= new();
                        questGroups.Add((quest, quest.ExclusiveGroup));
                    }
                }
            }
        }

        if (questGroups == null)
            return;

        HistoryHandler.DoAction(new AnonymousHistoryAction("Remove quests from group", () =>
        {
            foreach (var (quest, group) in questGroups)
            {
                if (group.Quests.Count == 0)
                    Elements.Insert(0, group);
                group.AddQuest(quest);
            }
        }, () =>
        {
            foreach (var (quest, group) in questGroups)
            {
                group.RemoveQuest(quest);
                if (group.Quests.Count == 0)
                    Elements.Remove(group);
            }
        }));
    }

    public void GroupSelectedQuests(ExclusiveGroupViewModel newGroup)
    {
        var questsToGroup = SelectedItems.Select(x => x as QuestViewModel)
            .Where(x => x != null)
            .Where(x => x!.ExclusiveGroup == null || !SelectedItems.Contains(x.ExclusiveGroup))
            .Where(x => x!.ExclusiveGroup != newGroup)
            .Select(x => (x!, x!.ExclusiveGroup))
            .ToList();

        if (questsToGroup.Count == 0)
            return;

        HistoryHandler.DoAction(new AnonymousHistoryAction("Group quests", () =>
        {
            foreach (var (quest, oldGroup) in questsToGroup)
            {
                newGroup.RemoveQuest(quest);
                if (oldGroup != null)
                {
                    if (oldGroup.Quests.Count == 0)
                        Elements.Insert(0, oldGroup);
                    oldGroup.AddQuest(quest);
                }
            }
        }, () =>
        {
            foreach (var (quest, oldGroup) in questsToGroup)
            {
                if (oldGroup != null)
                {
                    oldGroup.RemoveQuest(quest);
                    if (oldGroup.Quests.Count == 0)
                        Elements.Remove(oldGroup);
                }
                newGroup.AddQuest(quest!);
            }
        }));
    }

    public void DeleteConnections(List<ConnectionViewModel> connections)
    {
        var oldConnections = connections.Select(x => (x.FromNode, x.ToNode, x)).ToList();
        historyHandler.DoAction(new AnonymousHistoryAction("Delete connections", () =>
        {
            foreach (var (from, to, conn) in oldConnections)
            {
                conn.Attach(from, to);
                Connections.Add(conn);
            }
        }, () =>
        {
            foreach (var (from, to, conn) in oldConnections)
            {
                conn.Detach();
                Connections.Remove(conn);
            }
        }));
    }

    public void AddConnection(BaseQuestViewModel from, BaseQuestViewModel to, ConnectionType requirementType)
    {
        if (from == to)
            return;

        var conflicting = GetConflictingConnections(from, to, requirementType);
        var conflictingConnections = conflicting?.Select(x => (x.FromNode, x.ToNode, x))?.ToList();

        var connection = new ConnectionViewModel(requirementType, from, to);

        historyHandler.DoAction(new AnonymousHistoryAction("Connect nodes", () =>
        {
            connection.Detach();
            Connections.Remove(connection);
            if (conflictingConnections != null)
            {
                foreach (var (oldFrom, oldTo, oldConn) in conflictingConnections)
                {
                    oldConn.Attach(oldFrom, oldTo);
                    Connections.Add(oldConn);
                }
            }
        }, () =>
        {
            if (conflictingConnections != null)
            {
                foreach (var (oldFrom, oldTo, oldConn) in conflictingConnections)
                {
                    oldConn.Detach();
                    Connections.Remove(oldConn);
                }
            }

            connection.Attach(from, to);
            Connections.Add(connection);
        }));
    }

    public void CreateAndGroupSelectedQuests(QuestViewModel? otherQuest)
    {
        var questsToGroup = SelectedItems.Select(x => x as QuestViewModel)
            .Where(x => x != null)
            .Where(x => x!.ExclusiveGroup == null || !SelectedItems.Contains(x.ExclusiveGroup))
            .Concat(otherQuest == null ? Array.Empty<QuestViewModel>() : new[] {otherQuest})
            .Select(x => (x!, x!.ExclusiveGroup))
            .ToList();

        if (questsToGroup.Count == 0)
            return;

        var groupBounds = questsToGroup[0].Item1.Bounds;
        foreach (var q in questsToGroup)
            groupBounds = groupBounds.Union(q.Item1.Bounds);

        var newGroup = new ExclusiveGroupViewModel(this, QuestGroupType.All);

        HashSet<(BaseQuestViewModel, QuestRequirementType)> prerequisitesCommonForAllQuests = new();
        HashSet<(BaseQuestViewModel, QuestRequirementType)> requirementsCommonForAllQuests = new();

        foreach (var (quest, _) in questsToGroup)
        {
            foreach (var (to, type, conn) in quest.RequirementFor)
            {
                if (type == QuestRequirementType.Completed || type == QuestRequirementType.MustBeActive)
                {
                    if (questsToGroup.All(pair => pair.Item1.RequirementFor.Any(tuple => tuple.requirementFor == to && tuple.requirementType == type)))
                    {
                        requirementsCommonForAllQuests.Add((to, type));
                    }
                }
            }

            foreach (var (from, type, conn) in quest.Prerequisites)
            {
                if (type == QuestRequirementType.Completed || type == QuestRequirementType.MustBeActive)
                {
                    if (questsToGroup.All(pair => pair.Item1.Prerequisites.Any(tuple => tuple.prerequisite == from && tuple.requirementType == type)))
                    {
                        prerequisitesCommonForAllQuests.Add((from, type));
                    }
                }
            }
        }

        List<(ConnectionViewModel, BaseQuestViewModel, BaseQuestViewModel)> connectionsToRemove = new();
        List<(ConnectionViewModel, BaseQuestViewModel, BaseQuestViewModel)> connectionsToAdd = new();

        if (prerequisitesCommonForAllQuests.Count > 0)
        {
            HashSet<(BaseQuestViewModel, QuestRequirementType)> fromAdded = new();
            foreach (var (quest, _) in questsToGroup)
            {
                foreach (var (from, type, conn) in quest.Prerequisites)
                {
                    if (prerequisitesCommonForAllQuests.Contains((from, type)))
                    {
                        connectionsToRemove.Add((conn, from, quest));
                        if (fromAdded.Add((from, type)))
                            connectionsToAdd.Add((new ConnectionViewModel(type.ToConnectionType(), null, null), from, newGroup));
                    }
                }
            }
        }

        if (requirementsCommonForAllQuests.Count > 0)
        {
            HashSet<(BaseQuestViewModel, QuestRequirementType)> toAdded = new();
            foreach (var (quest, _) in questsToGroup)
            {
                foreach (var (to, type, conn) in quest.RequirementFor)
                {
                    if (requirementsCommonForAllQuests.Contains((to, type)))
                    {
                        connectionsToRemove.Add((conn, quest, to));
                        if (toAdded.Add((to, type)))
                            connectionsToAdd.Add((new ConnectionViewModel(type.ToConnectionType(), null, null), newGroup, to));
                    }
                }
            }
        }

        HistoryHandler.DoAction(new AnonymousHistoryAction("Group quests", () =>
        {
            Elements.Remove(newGroup);
            foreach (var (quest, oldGroup) in questsToGroup)
            {
                newGroup.RemoveQuest(quest);
                if (oldGroup != null)
                {
                    if (oldGroup.Quests.Count == 0)
                        Elements.Insert(0, oldGroup);
                    oldGroup.AddQuest(quest);
                }
            }

            foreach (var (conn, from, to) in connectionsToRemove)
            {
                conn.Attach(from, to);
                Connections.Add(conn);
            }

            foreach (var (conn, from, to) in connectionsToAdd)
            {
                conn.Detach();
                Connections.Remove(conn);
            }
        }, () =>
        {
            Elements.Insert(0, newGroup);
            newGroup.Location = new Point(groupBounds.Left - 10, groupBounds.Top - 10);
            newGroup.Bounds = new Rect(groupBounds.Left - 10, groupBounds.Top - 10, groupBounds.Width + 20, groupBounds.Height + 20);
            foreach (var (quest, oldGroup) in questsToGroup)
            {
                if (oldGroup != null)
                {
                    oldGroup.RemoveQuest(quest);
                    if (oldGroup.Quests.Count == 0)
                        Elements.Remove(oldGroup);
                }
                newGroup.AddQuest(quest);
            }

            foreach (var (conn, from, to) in connectionsToRemove)
            {
                conn.Detach();
                Connections.Remove(conn);
            }

            foreach (var (conn, from, to) in connectionsToAdd)
            {
                conn.Attach(from, to);
                Connections.Add(conn);
            }
        }));
    }

    public void SetExclusiveGroupType(ExclusiveGroupViewModel group, QuestGroupType newType)
    {
        var oldType = group.GroupType;
        if (oldType == newType)
            return;

        HistoryHandler.DoAction(new AnonymousHistoryAction("Change group type", () =>
            group.GroupType = oldType,
            () => group.GroupType = newType));
    }

    public void SetExclusiveGroupsType(IReadOnlyList<ExclusiveGroupViewModel> groups, QuestGroupType newType)
    {
        var oldTypes = groups.ToDictionary(x => x, x => x.GroupType);

        HistoryHandler.DoAction(new AnonymousHistoryAction("Change group type", () =>
            {
                foreach (var (group, type) in oldTypes)
                    group.GroupType = type;
            },
            () =>
            {
                foreach (var group in groups)
                    group.GroupType = newType;
            }));
    }

    public void SetQuestRace(QuestViewModel quest, CharacterRaces newRace)
    {
        var oldRace = quest.Races;
        if (oldRace == newRace)
            return;

        HistoryHandler.DoAction(new AnonymousHistoryAction("Change quest race", () =>
            quest.Races = oldRace,
            () => quest.Races = newRace));
    }

    public void SetQuestsRace(IReadOnlyList<QuestViewModel> quests, CharacterRaces newRace)
    {
        var oldRaces = quests.ToDictionary(x => x, x => x.Races);

        HistoryHandler.DoAction(new AnonymousHistoryAction("Change quest race", () =>
            {
                foreach (var (q, race) in oldRaces)
                    q.Races = race;
            },
            () =>
            {
                foreach (var q in quests)
                {
                    q.Races = newRace;
                }
            }));
    }

    public void SetQuestsClasses(IReadOnlyList<QuestViewModel> quests, CharacterClasses newClasses)
    {
        var oldRaces = quests.ToDictionary(x => x, x => x.Classes);

        HistoryHandler.DoAction(new AnonymousHistoryAction("Change quest classes", () =>
            {
                foreach (var (q, oldClasses) in oldRaces)
                    q.Classes = oldClasses;
            },
            () =>
            {
                foreach (var q in quests)
                {
                    q.Classes = newClasses;
                }
            }));
    }

    private void SetConnectionType(ConnectionViewModel conn, ConnectionType newType)
    {
        var oldType = conn.RequirementType;
        if (oldType == newType)
            return;

        HistoryHandler.DoAction(new AnonymousHistoryAction("Change connection type", () =>
            conn.RequirementType = oldType,
            () => conn.RequirementType = newType));
    }

    private void SetConnectionsType(IReadOnlyList<ConnectionViewModel> conns, QuestRequirementType newType)
    {
        var oldTypes = conns.ToDictionary(x => x, x => x.RequirementType);

        HistoryHandler.DoAction(new AnonymousHistoryAction("Change connection type", () =>
            {
                foreach (var (conn, type) in oldTypes)
                {
                    conn.RequirementType = type;
                }
            },
            () =>
            {
                foreach (var conn in conns)
                {
                    conn.RequirementType = newType.ToConnectionType();
                }
            }));
    }

    public void DoLayoutGraphNow()
    {
        var oldPositions = Elements.ToDictionary(x => x, x => x.Location);
        try
        {
            automaticGraphLayouter.DoLayoutNow(LayoutSettingsViewModel.CurrentAlgorithm!, Elements, Connections, true);
        }
        catch (Exception e)
        {
            LOG.LogWarning(e, "Failed to layout graph");
            return;
        }
        var newPositions = Elements.ToDictionary(x => x, x => new Point(x.PerfectX, x.PerfectY));

        HistoryHandler.DoAction(new AnonymousHistoryAction("Layout graph", () =>
        {
            foreach (var node in oldPositions)
            {
                node.Key.Location = node.Value;
                node.Key.PerfectX = node.Value.X;
                node.Key.PerfectY = node.Value.Y;
            }

            foreach (var node in oldPositions.Keys)
            {
                if (node is ExclusiveGroupViewModel group)
                    group.Arrange(default);
            }
        }, () =>
        {
            foreach (var node in newPositions)
            {
                node.Key.Location = node.Value;
                node.Key.PerfectX = node.Value.X;
                node.Key.PerfectY = node.Value.Y;
            }

            foreach (var node in newPositions.Keys)
            {
                if (node is ExclusiveGroupViewModel group)
                    group.Arrange(default);
            }
        }));
    }

    private void SetConditionsForQuests(IReadOnlyList<QuestViewModel> quests, List<ICondition> conditions)
    {
        var oldQuests = quests.ToDictionary(x => x, x => x.Conditions);

        historyHandler.DoAction(new AnonymousHistoryAction("Edit conditions", () =>
        {
            foreach (var (quest, oldConditions) in oldQuests)
                quest.Conditions = oldConditions;
        }, () =>
        {
            foreach (var quest in quests)
                quest.Conditions = conditions.Select(x => new AbstractCondition(x)).ToList();
        }));
    }

    private void DeleteGroups(IReadOnlyList<ExclusiveGroupViewModel> groups)
    {
        if (groups.Count == 0)
            return;

        var questsPerGroup = groups.ToDictionary(x => x, x => x.Quests.ToList());

        List<(ConnectionViewModel, BaseQuestViewModel, BaseQuestViewModel)> connectionsToRemove = new();
        List<(ConnectionViewModel, BaseQuestViewModel, BaseQuestViewModel)> connectionsToAdd = new();

        foreach (var group in groups)
        {
            foreach (var (from, type, conn) in group.Prerequisites)
            {
                foreach (var q in group.Quests)
                {
                    if (!q.Prerequisites.Any(tuple => tuple.prerequisite == from && tuple.requirementType == type))
                        connectionsToAdd.Add((new ConnectionViewModel(type.ToConnectionType()), from, q));
                }
                connectionsToRemove.Add((conn, from, group));
            }

            foreach (var (to, type, conn) in group.RequirementFor)
            {
                foreach (var q in group.Quests)
                {
                    if (!q.RequirementFor.Any(tuple => tuple.requirementFor == to && tuple.requirementType == type))
                        connectionsToAdd.Add((new ConnectionViewModel(type.ToConnectionType()), q, to));
                }
                connectionsToRemove.Add((conn, group, to));
            }
        }

        historyHandler.DoAction(new AnonymousHistoryAction("Delete groups", () =>
        {
            foreach (var (group, quests) in questsPerGroup)
            {
                Elements.Insert(0, group);
                foreach (var quest in quests)
                    group.AddQuest(quest);
            }

            foreach (var (conn, from, to) in connectionsToRemove)
            {
                conn.Attach(from, to);
                Connections.Add(conn);
            }

            foreach (var (conn, from, to) in connectionsToAdd)
            {
                conn.Detach();
                Connections.Remove(conn);
            }
        }, () =>
        {
            foreach (var (group, quests) in questsPerGroup)
            {
                foreach (var quest in quests)
                    group.RemoveQuest(quest);
                Elements.Remove(group);
            }

            foreach (var (conn, from, to) in connectionsToRemove)
            {
                conn.Detach();
                Connections.Remove(conn);
            }

            foreach (var (conn, from, to) in connectionsToAdd)
            {
                conn.Attach(from, to);
                Connections.Add(conn);
            }
        }));
    }

    private void UnloadConnectedNodesAndConnections(BaseQuestViewModel startQuest)
    {
        HashSet<BaseQuestViewModel> nodes = new();
        HashSet<ConnectionViewModel> connections = new();

        List<(BaseQuestViewModel from, BaseQuestViewModel to, ConnectionViewModel conn)> connectionsToRemove
            = new();

        void Traverse(BaseQuestViewModel vm)
        {
            if (!nodes.Add(vm))
                return;

            foreach (var conn in vm.RequirementFor)
            {
                Traverse(conn.requirementFor);
                if (connections.Add(conn.conn))
                {
                    connectionsToRemove.Add((vm, conn.requirementFor, conn.conn));
                }
            }

            foreach (var conn in vm.Prerequisites)
            {
                Traverse(conn.prerequisite);
                if (connections.Add(conn.conn))
                {
                    connectionsToRemove.Add((conn.prerequisite, vm, conn.conn));
                }
            }

            if (vm.FactionChange is { } otherFactionQuest)
            {
                Traverse(otherFactionQuest.quest);
                if (connections.Add(otherFactionQuest.conn))
                {
                    connectionsToRemove.Add((otherFactionQuest.conn.FromNode!, otherFactionQuest.conn.ToNode!, otherFactionQuest.conn));
                }
            }

            if (vm is ExclusiveGroupViewModel group)
            {
                foreach (var q in group.Quests)
                {
                    Traverse(q);
                }
            }
            else if (vm is QuestViewModel quest)
            {
                if (quest.ExclusiveGroup != null)
                {
                    Traverse(quest.ExclusiveGroup);
                }
            }
        }

        Traverse(startQuest);

        List<ChainRawData> affectedExistingData = new();
        List<(uint, IReadOnlyList<ICondition>)> affectedExistingConditions = new();

        foreach (var node in nodes.OfType<QuestViewModel>())
        {
            if (existingData.TryGetValue(node.Entry, out var data))
                affectedExistingData.Add(data);
            if (existingConditions.TryGetValue(node.Entry, out var conditions))
                affectedExistingConditions.Add((node.Entry, conditions));
        }

        var wasSavedBefore = History.IsSaved;

        historyHandler.DoAction(new AnonymousHistoryAction("Unload chain", () =>
        {
            foreach (var node in nodes)
            {
                if (node is ExclusiveGroupViewModel group)
                {
                    Elements.Insert(0, group);
                }
                else if (node is QuestViewModel quest)
                {
                    Elements.Add(node);
                    entryToQuest[quest.Entry] = quest;
                }
                else
                    throw new NotSupportedException($"Unsupported node type {node.GetType()}");
            }

            foreach (var data in affectedExistingData)
            {
                existingData[data.Id] = data;
            }

            foreach (var (quest, data) in affectedExistingConditions)
            {
                existingConditions[quest] = data;
            }

            foreach (var conn in connectionsToRemove)
            {
                conn.conn.Attach(conn.from, conn.to);
                Connections.Add(conn.conn);
            }
        }, () =>
        {
            foreach (var conn in connectionsToRemove)
            {
                conn.conn.Detach();
                Connections.Remove(conn.conn);
            }

            foreach (var node in nodes)
            {
                Elements.Remove(node);
                if (node is QuestViewModel quest)
                {
                    entryToQuest.Remove(quest.Entry);
                    existingData.Remove(quest.Entry);
                    existingConditions.Remove(quest.Entry);
                }
            }
        }));

        if (wasSavedBefore)
            History.MarkAsSaved();
    }

    private void MoveConnectionsToGroup(bool onlyOutgoing, IReadOnlyList<QuestViewModel> quests)
    {
        List<(ConnectionViewModel, BaseQuestViewModel, BaseQuestViewModel)> connectionsToRemove = new();
        List<(ConnectionViewModel, BaseQuestViewModel, BaseQuestViewModel)> connectionsToAdd = new();

        bool ConnectionExists(BaseQuestViewModel from, BaseQuestViewModel to)
        {
            return from.RequirementFor.Any(tuple => tuple.requirementFor == to) ||
                   connectionsToAdd.Any(tuple => tuple.Item2 == from && tuple.Item3 == to);
        }

        foreach (var quest in quests.Where(q => q.ExclusiveGroup != null))
        {
            if (onlyOutgoing)
            {
                foreach (var (requirementFor, requirementType, conn) in quest.RequirementFor)
                {
                    if (!ConnectionExists(quest.ExclusiveGroup!, requirementFor))
                    {
                        var newConnection = new ConnectionViewModel(requirementType.ToConnectionType(),quest.ExclusiveGroup, requirementFor);
                        connectionsToAdd.Add((newConnection, quest.ExclusiveGroup!, requirementFor));
                    }
                    connectionsToRemove.Add((conn, quest, requirementFor));
                }
            }
            else
            {
                foreach (var (prerequisite, requirementType, conn) in quest.Prerequisites)
                {
                    if (!ConnectionExists(prerequisite, quest.ExclusiveGroup!))
                    {
                        var newConnection = new ConnectionViewModel(requirementType.ToConnectionType(), prerequisite, quest.ExclusiveGroup);
                        connectionsToAdd.Add((newConnection, prerequisite, quest.ExclusiveGroup!));
                    }

                    connectionsToRemove.Add((conn, prerequisite, quest));
                }
            }
        }

        if (connectionsToAdd.Count == 0 && connectionsToRemove.Count == 0)
            return;

        historyHandler.DoAction(new AnonymousHistoryAction("Move connections to group", () =>
        {
            foreach (var (conn, from, to) in connectionsToAdd)
            {
                conn.Detach();
                Connections.Remove(conn);
            }
            foreach (var (conn, from, to) in connectionsToRemove)
            {
                conn.Attach(from, to);
                Connections.Add(conn);
            }
        }, () =>
        {
            foreach (var (conn, from, to) in connectionsToRemove)
            {
                conn.Detach();
                Connections.Remove(conn);
            }
            foreach (var (conn, from, to) in connectionsToAdd)
            {
                conn.Attach(from, to);
                Connections.Add(conn);
            }
        }));
    }
}