using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaGraph.GraphLayout;
using AvaloniaGraph.ViewModels;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Layout.Incremental;
using Prism.Commands;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.QuestChainEditor.Documents;
using WDE.QuestChainEditor.Models;
using WDE.QuestChainEditor.QueryGenerators;

namespace WDE.QuestChainEditor.ViewModels;

[AutoRegister]
public class QuestChainDocumentViewModel : ObservableBase, ISolutionItemDocument
{
    private readonly IQuestTemplateSource questTemplateSource;
    private readonly IQueryGenerator queryGenerator;
    private readonly ISqlGenerator sqlGenerator;
    private readonly IMessageBoxService messageBoxService;
    private readonly IMiniIcons miniIcons;
    private readonly QuestPickerService questPickerService;
    private readonly QuestChainSolutionItem solutionItem;
    
    public ObservableCollection<QuestViewModel> Elements { get; } = new();
    public ObservableCollection<QuestConnectionViewModel> Connections { get; } = new();
    public List<QuestConnectionViewModel> SelectedConnections { get; } = new();

    private Dictionary<uint, QuestViewModel> entryToQuest = new();
    private Dictionary<uint, ChainRawData> existingData = new();
    private AlternativeTreeLayout a = new AlternativeTreeLayout();

    private QuestViewModel GetOrCreate(Quest q)
    {
        if (entryToQuest.TryGetValue(q.Entry, out var quest))
            return quest;
        quest = entryToQuest[q.Entry] = new(q.Template, miniIcons);
        Elements.Add(quest);
        return quest;
    }

    public QuestChainDocumentViewModel(QuestChainSolutionItem solutionItem,
        IHistoryManager history, IQuestTemplateSource questTemplateSource,
        IQueryGenerator queryGenerator,
        ISqlGenerator sqlGenerator,
        ITaskRunner taskRunner,
        IMySqlExecutor mySqlExecutor,
        IDatabaseProvider databaseProvider,
        QuestPickerViewModel questPicker,
        IMessageBoxService messageBoxService,
        IMiniIcons miniIcons,
        QuestPickerService questPickerService)
    {
        this.questTemplateSource = questTemplateSource;
        this.queryGenerator = queryGenerator;
        this.sqlGenerator = sqlGenerator;
        this.messageBoxService = messageBoxService;
        this.miniIcons = miniIcons;
        this.questPickerService = questPickerService;
        this.QuestPicker = questPicker;
        SolutionItem = this.solutionItem = solutionItem;
        existingData = solutionItem.ExistingData.ToDictionary(e => e.Key, e=>e.Value);
        History = history;
        Undo = history.UndoCommand();
        Redo = history.RedoCommand();

        QuestPicker.CloseCancel += () =>
        {
            questPickingTask?.SetResult(null);
            questPickingTask = null;
            isPickingQuest = false;
            RaisePropertyChanged(nameof(IsPickingQuest));
        };
        QuestPicker.CloseOk += () =>
        {
            questPickingTask?.SetResult(QuestPicker.SelectedQuest);
            questPickingTask = null;
            isPickingQuest = false;
            RaisePropertyChanged(nameof(IsPickingQuest));
        };

        Save = new AsyncAutoCommand(async () =>
        {
            var store = BuildCurrentQuestStore();
            var query = queryGenerator.Generate(store);
            var sql = sqlGenerator.GenerateQuery(query.ToList(), null); // passing null as 'existing' to generate all
            await taskRunner.ScheduleTask("Save chain", async () =>
            {
                await mySqlExecutor.ExecuteSql(sql);
            });
        });

        DeleteSelected = new DelegateCommand(() =>
        {
            foreach (var conn in SelectedConnections.ToList())
            {
                conn.Detach();
                Connections.Remove(conn);
            }
        });

        // HashSet<uint> done = new();
        // List<(int, uint, string)> top = new();
        // foreach (var q in databaseProvider.GetQuestTemplates())
        // {
        //     if (!done.Add(q.Entry))
        //         continue;
        //     var questStore = new QuestStore(questTemplateSource);
        //     questStore.LoadQuest(q.Entry);
        //     foreach (var quest in questStore)
        //     {
        //         done.Add(quest.Entry);
        //     }
        //     top.Add((questStore.Count(), q.Entry, q.Name));
        // }
        // top.Sort();
        //
        // for (int i = top.Count - 1; i >= top.Count - 100; --i)
        // {
        //     Console.WriteLine(top[i].Item1 + " " + top[i].Item2 + " " + top[i].Item3);
        // }
        
        LoadQuestWithDependencies(100000);

        DispatcherTimer.Run(Update, TimeSpan.FromMilliseconds(16));
    }

    private QuestViewModel LoadQuestWithDependencies(uint quest)
    {
        QuestViewModel? mainQuest = null;
        
        var quests = new QuestStore(questTemplateSource);
        quests.LoadQuest(quest);
        foreach (var q in quests)
        {
            var template = questTemplateSource.GetTemplate(q.Entry);
            if (!existingData.ContainsKey(q.Entry))
                existingData[q.Entry] = new ChainRawData(template!);
            
            var viewModel = GetOrCreate(q);
            if (q.Entry == quest)
                mainQuest = viewModel;

            if (q.Requirements.Count == 1 && q.Requirements[0].RequirementType == QuestRequirementType.AllCompleted)
            {
                foreach (var requiredQuestEntry in q.Requirements[0].Quests)
                {
                    var requiredQuest = GetOrCreate(quests[requiredQuestEntry]);
                    var connection = new QuestConnectionViewModel(requiredQuest.IsRequiredByConnector, viewModel.RequiresConnector, QuestConnectionType.Hierarchy);
                    Connections.Add(connection);
                }
            }
            else
            {
                QuestViewModel? prev = null;
                QuestConnectionType type;
                foreach (var requirementGroup in q.Requirements)
                {
                    type = QuestConnectionType.Or;
                    foreach (var requiredQuestEntry in requirementGroup.Quests)
                    {
                        var requiredQuest = GetOrCreate(quests[requiredQuestEntry]);
                        var connection = new QuestConnectionViewModel(requiredQuest.IsRequiredByConnector, viewModel.RequiresConnector, QuestConnectionType.Hierarchy);
                        Connections.Add(connection);

                        if (prev != null)
                        {
                            connection = new QuestConnectionViewModel(prev.RightOutputConnector, requiredQuest.LeftInputConnector, type);
                            Connections.Add(connection);
                        }

                        if (requirementGroup.RequirementType == QuestRequirementType.AllCompleted)
                            type = QuestConnectionType.And;
                        else if (requirementGroup.RequirementType == QuestRequirementType.MustBeActive)
                            type = QuestConnectionType.And;
                        else if (requirementGroup.RequirementType == QuestRequirementType.OnlyOneCompleted ||
                                 requirementGroup.RequirementType == QuestRequirementType.Breadcrumb)
                            type = QuestConnectionType.OneOf;

                        prev = requiredQuest;
                    }
                }
            }
        }

        return mainQuest!;
    }

    private QuestStore BuildCurrentQuestStore()
    {
        Dictionary<uint, Quest> quests = new();
        foreach (var questViewModel in Elements)
        {
            if (!quests.TryGetValue(questViewModel.Entry, out var questModel))
                questModel = quests[questViewModel.Entry] = new(questTemplateSource.GetTemplate(questViewModel.Entry)!);
            
            if (questViewModel.RequiresConnector.Connections.Count == 0)
                continue;
            
            var required = questViewModel.RequiresConnector.Connections.Select(cn => cn.From!.Node).ToList();
            
            questModel.AddRequirement(new QuestGroup(QuestRequirementType.AllCompleted, 1, required.Select(s => s.Entry).ToArray()));
        }
        var store = new QuestStore(questTemplateSource, quests);
        return store;
    }
    
    public async Task<string> GenerateQuery()
    {
        var store = BuildCurrentQuestStore();
        var query = queryGenerator.Generate(store);
        var sql = sqlGenerator.GenerateQuery(query.ToList(), existingData);
        return sql.QueryString;
    }

    public bool AutoLayout
    {
        get => autoLayout;
        set => SetProperty(ref autoLayout, value);
    }

    private QuestViewModel GetLeftMostNode(QuestViewModel quest)
    {
        QuestViewModel left = quest;
        while (left.LeftInputConnector.Connections.Count == 1 &&
               left.LeftInputConnector.Connections[0].From != null)
        {
            left = left.LeftInputConnector.Connections[0].From!.Node;
        }

        return left;
    }
    
    private bool Update()
    {
        if (!autoLayout)
            return true;
        
        List<QuestViewModel> roots = Elements.Where(e => e.RequiresConnector.Connections.Count == 0 ||
                                                         e.RequiresConnector.Connections[0].From == null).ToList();
        if (roots == null || roots.Count == 0)
            return true;
        
        a.DoLayout(roots);

        foreach (var e in Elements)
        {
            QuestViewModel left = GetLeftMostNode(e);

            QuestViewModel? node = left;
            double max = double.MinValue;
            while (node != null)
            {
                max = Math.Max(max, node.PerfectY);
                node = node.RightOutputConnector.Connections.Count == 1 ? node.RightOutputConnector.Connections[0].To?.Node : null;
            }
            
            node = left;
            while (node != null)
            {
                node.PerfectY = max;
                node = node.RightOutputConnector.Connections.Count == 1 ? node.RightOutputConnector.Connections[0].To?.Node : null;
            }
        }

        
        foreach (var e in Elements)
            e.Force = new Vector2((float)e.X - (float)e.PerfectX, (float)e.Y - (float)e.PerfectY) * -1 * 1.1f;

        for (var index = 0; index < Elements.Count; index++)
        {
            var e1 = Elements[index];
            for (var i = index + 1; i < Elements.Count; i++)
            {
                var e2 = Elements[i];
                var pos = new Vector2((float)e1.X, (float)e1.Y);
                var dir = new Vector2((float)e2.X, (float)e2.Y) - pos;
                var force = dir / Math.Max(dir.LengthSquared(), 0.01f);
                force *= new Vector2(10000, 10000);
                if (e1.Y > e2.Y)
                    force *= new Vector2(1, -1f);
                //e1.Force += force * -1;
                //e2.Force += force;
            }
        }
        
        foreach (var e in Elements)
        {
            if (e.IsDragging)
                continue;
            e.X += e.Force.X * 0.05f;
            e.Y += e.Force.Y * 0.05f;
        }
        
        return true;
    }

    #region Draggin

    private bool connectingTo;
    private bool connectingHierarchy;
    private Point currentDragPoint;
    private bool isPickingQuest;

    public QuestConnectionViewModel? OnConnectionDragStarted(ConnectorViewModel<QuestViewModel, QuestConnectionViewModel> sourceConnector, Point currentDragPoint)
    {
        if (sourceConnector == sourceConnector.Node.IsRequiredByConnector)
        {
            connectingTo = true;
            connectingHierarchy = true;
            QuestConnectionViewModel connection = new(sourceConnector.Node.IsRequiredByConnector, QuestConnectionType.Hierarchy)
            {
                ToPosition = currentDragPoint
            };

            Connections.Add(connection);

            return connection;
        }

        if (sourceConnector == sourceConnector.Node.RequiresConnector)
        {
            connectingTo = false;
            connectingHierarchy = true;
            QuestConnectionViewModel connection = new(sourceConnector.Node.RequiresConnector, QuestConnectionType.Hierarchy)
            {
                FromPosition = currentDragPoint
            };

            Connections.Add(connection);

            return connection;
        }
        
        if (sourceConnector == sourceConnector.Node.RightOutputConnector)
        {
            connectingTo = true;
            connectingHierarchy = false;
            QuestConnectionViewModel connection = new(sourceConnector.Node.RightOutputConnector, QuestConnectionType.And)
            {
                FromPosition = currentDragPoint
            };

            Connections.Add(connection);

            return connection;
        }

        if (sourceConnector == sourceConnector.Node.LeftInputConnector)
        {
            connectingTo = false;
            connectingHierarchy = false;
            QuestConnectionViewModel connection = new(sourceConnector.Node.LeftInputConnector, QuestConnectionType.And)
            {
                ToPosition = currentDragPoint
            };

            Connections.Add(connection);

            return connection;
        }

        return null;
    }

    public void OnConnectionDragging(Point currentDragPoint, QuestConnectionViewModel connection)
    {
        // If current drag point is close to an input connector, show its snapped position.
        ConnectorViewModel<QuestViewModel, QuestConnectionViewModel>? nearbyConnector = null;

        if (connectingHierarchy)
        {
            if (connectingTo)
                nearbyConnector = FindNearbyInputConnector(connection, currentDragPoint);
            else
                nearbyConnector = FindNearbyOutputConnector(connection, currentDragPoint);   
        }
        else
        {
            nearbyConnector = FindNearbyNonHierarchyConnector(connection, currentDragPoint);
        }

        var pnt = nearbyConnector?.Position ?? currentDragPoint;

        if (connectingTo)
            connection.ToPosition = pnt;
        else
            connection.FromPosition = pnt;
    }

    private bool AddNonHierarchyConnection(QuestViewModel from, QuestViewModel to, QuestConnectionType type)
    {
        if (from == to)
            return false;

        if (DoesNewEdgeIntroducesCycle(from, to, true, true)||
            DoesNewEdgeIntroducesCycle(to, from, true, true))
            return false;
        
        while (from.RightOutputConnector.NonEmpty)
        {
            Connections.Remove(from.RightOutputConnector.Connections[0]);
            from.RightOutputConnector.Connections[0].Detach();
        }
        while (to.LeftInputConnector.NonEmpty)
        {
            Connections.Remove(to.LeftInputConnector.Connections[0]);
            to.LeftInputConnector.Connections[0].Detach();
        }
        
        var connection = new QuestConnectionViewModel(from.RightOutputConnector, to.LeftInputConnector, type);
        Connections.Add(connection);
        return true;
    }
    
    private bool AddConnection(QuestViewModel from, QuestViewModel to)
    {
        if (from == to)
            return false;

        if (DoesNewEdgeIntroducesCycle(from, to))
            return false;
            
        RemoveConflictingConnections(to, from);
        var connection = new QuestConnectionViewModel(from.IsRequiredByConnector, to.RequiresConnector, QuestConnectionType.Hierarchy);
        Connections.Add(connection);
        return true;
    }

    public void OnConnectionDragCompleted(Point currentDragPoint,
        QuestConnectionViewModel newConnection,
        ConnectorViewModel<QuestViewModel, QuestConnectionViewModel> sourceConnector)
    {
        var fromNewConnection = newConnection.From?.Node!;
        var toNewConnection = newConnection.To?.Node!;

        newConnection.Detach();
        Connections.Remove(newConnection);

        this.currentDragPoint = currentDragPoint;

        if (connectingHierarchy)
        {
            if (connectingTo)
            {
                var nearbyConnector = FindNearbyInputConnector(newConnection, currentDragPoint);

                if (nearbyConnector == null)
                    AsyncTryConnect((sourceConnector as OutputConnectorViewModel<QuestViewModel, QuestConnectionViewModel>)!, newConnection.ToPosition).ListenErrors();
                else
                   AddConnection(sourceConnector.Node, nearbyConnector.Node);
            }
            else
            {
                var nearbyConnector = FindNearbyOutputConnector(newConnection, currentDragPoint);

                if (nearbyConnector == null)
                    AsyncTryConnectTo((sourceConnector as InputConnectorViewModel<QuestViewModel, QuestConnectionViewModel>)!, newConnection.FromPosition).ListenErrors();
                else
                    AddConnection(nearbyConnector.Node, sourceConnector.Node);
            }   
        }
        else
        {
            var nearbyConnector = FindNearbyNonHierarchyConnector(newConnection, currentDragPoint);
            if (nearbyConnector != null)
            {
                if (nearbyConnector == nearbyConnector.Node.LeftInputConnector)
                    AddNonHierarchyConnection(sourceConnector.Node, nearbyConnector.Node, QuestConnectionType.And);
                else
                    AddNonHierarchyConnection(nearbyConnector.Node, sourceConnector.Node, QuestConnectionType.And);
            }
        }
    }

    private bool DoesNewEdgeIntroducesCycle(QuestViewModel from, QuestViewModel to, bool includingSiblings = false, bool bothWays = false)
    {
        if (IsElementInChildren(from, to) ||
            (bothWays && IsElementInChildren(to, from)))
        {
            messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                .SetTitle("Invalid connection")
                .SetMainInstruction("This connection introduces a cycle.")
                .SetContent("You can't have cycles in any quest chain.")
                .SetIcon(MessageBoxIcon.Warning)
                .WithOkButton(true)
                .Build()).ListenErrors();
            return true;
        }

        if (includingSiblings)
        {
            var left = GetLeftMostNode(from);
            while (left != null)
            {
                if (DoesNewEdgeIntroducesCycle(left, to, false, bothWays))
                    return true;
                if (left.RightOutputConnector.Connections.Count == 0)
                    left = null;
                else
                    left = left.RightOutputConnector.Connections[0].To?.Node;
            }
        }

        return false;
    }

    private void RemoveConflictingConnections(QuestViewModel newChild, QuestViewModel newParent)
    {
        var inputConnections = newChild.RequiresConnector.Connections;
        for (var index = inputConnections.Count - 1; index >= 0; index--)
        {
            var existing = inputConnections[index] as QuestConnectionViewModel;
            if (IsOnInputsPath(newParent, existing!.FromNode) || IsElementInChildren(newParent, existing.FromNode))
            {
                existing.Detach();
                Connections.Remove(existing);
            }
        }
    }
    
    private async Task AsyncTryConnectTo(InputConnectorViewModel<QuestViewModel, QuestConnectionViewModel> destination, Point newConnectionToPosition)
    {
        var id = await PickQuest();
        if (!id.HasValue)
            return;

        var existing = Elements.FirstOrDefault(e => e.Entry == id.Value);
        if (existing != null)
        {
            AddConnection(existing, destination.Node);
        }
        else
        {
            var newQuest = LoadQuestWithDependencies(id.Value);
        
            newQuest.X = newConnectionToPosition.X;
            newQuest.Y = newConnectionToPosition.Y;
        
            var connection = new QuestConnectionViewModel(newQuest.IsRequiredByConnector, destination, QuestConnectionType.Hierarchy);
            Connections.Add(connection);
        }
    }
    
    private async Task AsyncTryConnect(OutputConnectorViewModel<QuestViewModel, QuestConnectionViewModel> source, Point newConnectionToPosition)
    {
        var id = await PickQuest();
        if (!id.HasValue)
            return;

        var existing = Elements.FirstOrDefault(e => e.Entry == id.Value);
        if (existing != null)
        {
            AddConnection(source.Node, existing);
        }
        else
        {
            var newQuest = LoadQuestWithDependencies(id.Value);
        
            newQuest.X = newConnectionToPosition.X;
            newQuest.Y = newConnectionToPosition.Y;
        
            var connection = new QuestConnectionViewModel(source, newQuest.RequiresConnector, QuestConnectionType.Hierarchy);
            Connections.Add(connection);
        }
    }

    private bool IsElementInChildren(ITreeNode nodeToFind, QuestViewModel? nodeToFindIn)
    {
        if (nodeToFindIn == null)
            return false;
        
        Queue<QuestViewModel> leafs = new Queue<QuestViewModel>();
        leafs.Enqueue(nodeToFindIn);

        while (leafs.Count > 0)
        {
            nodeToFindIn = leafs.Dequeue();
            if (nodeToFindIn == nodeToFind)
                return true;
            foreach (var conn in nodeToFindIn.IsRequiredByConnector.Connections)
            {
                if (conn.ToNode != null)
                    leafs.Enqueue((QuestViewModel)conn.ToNode);
            }
        }

        return false;
    }

    private bool IsOnInputsPath(ITreeNode nodeToFind, QuestViewModel? leaf)
    {
        if (leaf == null)
            return false;
        
        Queue<QuestViewModel> leafs = new Queue<QuestViewModel>();
        leafs.Enqueue(leaf);
        
        while (leafs.Count > 0)
        {
            leaf = leafs.Dequeue();
            if (leaf == nodeToFind)
                return true;
            foreach (var parent in leaf.RequiresConnector.Connections)
            {
                if (parent.FromNode == null)
                    continue;
                leafs.Enqueue(parent.FromNode);
            }
        }

        return false;
    }

    private T? FindNearby<T>(Point position, Func<QuestViewModel, ConnectorViewModel<QuestViewModel, QuestConnectionViewModel>> getConnector)
        where T : ConnectorViewModel<QuestViewModel, QuestConnectionViewModel>
    {
        foreach (var element in Elements)
        {
            var connector = getConnector(element);
            if (AreClose(connector.Position, position, 20))
                return (T)connector;
        }

        return null;
    }
    
    private InputConnectorViewModel<QuestViewModel, QuestConnectionViewModel>? FindNearbyInputConnector(QuestConnectionViewModel connection, Point mousePosition)
    {
        return FindNearby<InputConnectorViewModel<QuestViewModel, QuestConnectionViewModel>>(mousePosition, x => x.RequiresConnector);
    }

    private OutputConnectorViewModel<QuestViewModel, QuestConnectionViewModel>? FindNearbyOutputConnector(QuestConnectionViewModel connection, Point mousePosition)
    {
        return FindNearby<OutputConnectorViewModel<QuestViewModel, QuestConnectionViewModel>>(mousePosition, x => x.IsRequiredByConnector);
    }
    
    private ConnectorViewModel<QuestViewModel, QuestConnectionViewModel>? FindNearbyNonHierarchyConnector(QuestConnectionViewModel connection, Point mousePosition)
    {
        var nearby = FindNearby<ConnectorViewModel<QuestViewModel, QuestConnectionViewModel>>(mousePosition, x => x.LeftInputConnector);
        if (nearby != null)
            return nearby;
        return FindNearby<ConnectorViewModel<QuestViewModel, QuestConnectionViewModel>>(mousePosition, x => x.RightOutputConnector);
    }

    private static bool AreClose(Point point1, Point point2, double distance)
    {
        var d = point1 - point2;
        return d.X * d.X + d.Y * d.Y < distance * distance;
    }

    #endregion
    
    public ICommand DeleteSelected { get; }
    public ICommand Undo { get; set; }
    public ICommand Redo { get; set; }
    public IHistoryManager? History { get; set; }
    public bool IsModified { get; set; }
    public string Title => "Quest chain";
    public ICommand Copy => AlwaysDisabledCommand.Command;
    public ICommand Cut => AlwaysDisabledCommand.Command;
    public ICommand Paste => AlwaysDisabledCommand.Command;
    public ICommand Save { get; }
    public IAsyncCommand? CloseCommand { get; set; }
    public bool CanClose => true;
    public ISolutionItem SolutionItem { get; set; }

    public QuestPickerViewModel QuestPicker { get; }

    private TaskCompletionSource<uint?>? questPickingTask;
    private bool autoLayout = true;

    public bool IsPickingQuest
    {
        get => isPickingQuest;
        set
        {
            if (!value && questPickingTask != null)
            {
                questPickingTask.SetResult(null);
                questPickingTask = null;
            }
            SetProperty(ref isPickingQuest, value);
        }
    }

    protected Task<uint?> PickQuest()
    {
        questPickingTask = new();
        QuestPicker.Reset();
        IsPickingQuest = true;
        return questPickingTask.Task;
    }
}