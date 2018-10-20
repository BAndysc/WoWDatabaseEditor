using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using WDE.QuestChainEditor.Models;
using WDE.QuestChainEditor.Providers;

namespace WDE.QuestChainEditor.Editor.ViewModels
{
    public class GraphViewModel : BindableBase
    {
        public ObservableCollection<ElementViewModel> Elements { get; }
        
        public ObservableCollection<ConnectionViewModel> Connections { get; }

        public IEnumerable<ElementViewModel> SelectedElements => Elements.Where(x => x.IsSelected);

        public IEnumerable<ConnectionViewModel> SelectedConnections => Connections.Where(x => x.IsSelected);

        public event Action<ConnectionViewModel> RequestNodePickerWindow = delegate { };

        private Dictionary<Quest, ElementViewModel> QuestToViweModel = new Dictionary<Quest, ElementViewModel>();
        private readonly IQuestPicker picker;
        private readonly QuestList quests;

        public GraphViewModel(IQuestPicker picker, QuestList quests)
        {
            Elements = new ObservableCollection<ElementViewModel>();
            Connections = new ObservableCollection<ConnectionViewModel>();

            quests.OnAddedQuest += Quests_OnAddedQuest;
            quests.OnRemovedQuest += Quests_OnRemovedQuest;

            foreach (var quest in quests)
                OnQuestAddedToCollection(quest);
            this.picker = picker;
            this.quests = quests;
        }

        private void Quests_OnRemovedQuest(QuestList arg1, Quest quest)
        {
            DeleteElement(QuestToViweModel[quest]);
            QuestToViweModel.Remove(quest);
        }

        private void Quests_OnAddedQuest(QuestList arg1, Quest quest)
        {
            OnQuestAddedToCollection(quest);
        }

        private void OnRequiredQuestAdded(Quest quest, Quest dependency)
        {
            if (!QuestToViweModel.ContainsKey(quest))
                throw new Exception("Fatal error - quest should be in list");

            if (!QuestToViweModel.ContainsKey(dependency))
                OnQuestAddedToCollection(dependency);

            var vmReq = QuestToViweModel[quest];
            var vmDep = QuestToViweModel[dependency];

            Connections.Add(new ConnectionViewModel(vmDep.OutputConnectors[0], vmReq.InputConnectors[0]));
        }

        private void OnQuestAddedToCollection(Quest quest)
        {
            if (QuestToViweModel.ContainsKey(quest))
                return;

            var vm = new QuestViewModel(quest);
            AddElement(vm, UseCurrentDragPoint ? CurrentDragPoint.X : 10000, UseCurrentDragPoint ? CurrentDragPoint.Y : 10000);
            QuestToViweModel.Add(quest, vm);

            foreach (var dep in quest.RequiredQuests)
                OnRequiredQuestAdded(quest, dep);

            quest.RequiredQuests.CollectionChanged += (sender, e) =>
            {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                {
                    foreach (Quest dependency in e.NewItems)
                        OnRequiredQuestAdded(quest, dependency);
                }
                else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                {
                    foreach (Quest dependency in e.OldItems)
                    {
                        var depVM = QuestToViweModel[dependency];

                        var connection = vm.InputConnectors[0].Connections.Where(t => t.From == depVM.OutputConnectors[0]).FirstOrDefault();

                        connection.Detach();
                        Connections.Remove(connection);
                    }
                }
             };
        }

        public ElementViewModel AddElement(ElementViewModel model, double x, double y)
        {
            model.X = x;
            model.Y = y;
            Elements.Add(model);
            return model;
        }

        public void ShowPicker(Point point)
        {
            CurrentDragPoint = point;

            var quest = picker.ChooseQuest();
            if (quest == null)
                return;
            var q = new Quest(quest.Id, quest.Title);
            UseCurrentDragPoint = true;
            quests.AddQuest(q);
            UseCurrentDragPoint = false;
        }

        private bool connectingTo = false;
        public ConnectionViewModel OnConnectionDragStarted(ConnectorViewModel sourceConnector, Point currentDragPoint)
        {
            if (sourceConnector is OutputConnectorViewModel)
            {
                connectingTo = true;
                var connection = new ConnectionViewModel((OutputConnectorViewModel)sourceConnector)
                {
                    ToPosition = currentDragPoint
                };

                Connections.Add(connection);

                return connection;
            }

            else if (sourceConnector is InputConnectorViewModel)
            {
                connectingTo = false;
                var connection = new ConnectionViewModel((InputConnectorViewModel)sourceConnector)
                {
                    FromPosition = currentDragPoint
                };

                Connections.Add(connection);

                return connection;
            }
            return null;
        }

        public void OnConnectionDragging(Point currentDragPoint, ConnectionViewModel connection)
        {
            // If current drag point is close to an input connector, show its snapped position.
            ConnectorViewModel nearbyConnector = null;

            if (connectingTo)
                nearbyConnector = FindNearbyInputConnector(connection, currentDragPoint);
            else
                nearbyConnector = FindNearbyOutputConnector(connection, currentDragPoint); 

            Point pnt = (nearbyConnector != null)
                    ? nearbyConnector.Position
                    : currentDragPoint;

            if (connectingTo)
                connection.ToPosition = pnt;
            else
                connection.FromPosition = pnt;
        }
        
        public ConnectionViewModel pendingConnection;
        Point CurrentDragPoint;
        private bool UseCurrentDragPoint;

        public void OnConnectionDragCompleted(Point currentDragPoint, ConnectionViewModel newConnection, ConnectorViewModel sourceConnector)
        {
            var fromNewConnection = (QuestViewModel)(newConnection.From?.Element);
            var toNewConnection = (QuestViewModel)(newConnection.To?.Element);

            newConnection.Detach();
            Connections.Remove(newConnection);

            CurrentDragPoint = currentDragPoint;
            if (connectingTo)
            {
                var nearbyConnector = FindNearbyInputConnector(newConnection, currentDragPoint);

                if (nearbyConnector == null)
                {
                    var quest = picker.ChooseQuest();
                    if (quest == null)
                        return;
                    var q = new Quest(quest.Id, quest.Title);
                    UseCurrentDragPoint = true;
                    bool added = quests.AddQuest(q);
                    UseCurrentDragPoint = false;
                    if (added)
                        q.RequiredQuests.Add(fromNewConnection.quest);
                    return;
                }

                if (nearbyConnector == null || sourceConnector.Element == nearbyConnector.Element)
                    return;

                var to = ((QuestViewModel)nearbyConnector.Element).quest;
                var from = fromNewConnection.quest;
                to.RequiredQuests.Add(from);
            }
            else
            {
                var nearbyConnector = FindNearbyOutputConnector(newConnection, currentDragPoint);
                
                if (nearbyConnector == null)
                {
                    var quest = picker.ChooseQuest();
                    if (quest == null)
                        return;
                    var q = new Quest(quest.Id, quest.Title);
                    UseCurrentDragPoint = true;
                    bool added = quests.AddQuest(q);
                    UseCurrentDragPoint = false;
                    if (added)
                        toNewConnection.quest.RequiredQuests.Add(q);
                    return;
                }

                if (nearbyConnector == null || sourceConnector.Element == nearbyConnector.Element)
                    return;

                var to = toNewConnection.quest;
                var from = ((QuestViewModel)nearbyConnector.Element).quest;
                to.RequiredQuests.Add(from);
            }
        }

        private InputConnectorViewModel FindNearbyInputConnector(ConnectionViewModel connection, Point mousePosition)
        {
            return Elements.SelectMany(x => x.InputConnectors)
                .FirstOrDefault(x => AreClose(x.Position, mousePosition, 10));
        }

        private OutputConnectorViewModel FindNearbyOutputConnector(ConnectionViewModel connection, Point mousePosition)
        {
            return Elements.SelectMany(x => x.OutputConnectors)
                .FirstOrDefault(x => AreClose(x.Position, mousePosition, 10));
        }

        private static bool AreClose(Point point1, Point point2, double distance)
        {
            return (point1 - point2).Length < distance;
        }

        private void DeleteElement(ElementViewModel element)
        {
            foreach (var item in element.AttachedConnections)
                Connections.Remove(item);
            Elements.Remove(element);
        }
        
        public void DeleteSelectedElements()
        {
            Elements.Where(x => x.IsSelected).Select(x => ((QuestViewModel)x).quest)
                .ToList()
                .ForEach(x => quests.RemoveQuest(x));
        }

        public void DeleteSelectedConnections()
        {
            foreach (var connection in Connections.Where(x => x.IsSelected).ToList())
            {
                var from = ((QuestViewModel)connection.From.Element).quest;
                var to = ((QuestViewModel)connection.To.Element).quest;

                to.RequiredQuests.Remove(from);
            }
        }
        
    }
}
