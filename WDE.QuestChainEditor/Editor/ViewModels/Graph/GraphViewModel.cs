using WDE.QuestChainEditor.Models;
using WDE.QuestChainEditor.Providers;

namespace WDE.QuestChainEditor.Editor.ViewModels
{
    public class GraphViewModel : BindableBase
    {
        private readonly IQuestPicker picker;
        private readonly QuestList quests;

        private readonly Dictionary<Quest, ElementViewModel> questToViweModel = new();

        private bool connectingTo;
        private Point currentDragPoint;

        public ConnectionViewModel PendingConnection;
        private bool useCurrentDragPoint;

        public GraphViewModel(IQuestPicker picker, QuestList quests)
        {
            Elements = new ObservableCollection<ElementViewModel>();
            Connections = new ObservableCollection<ConnectionViewModel>();

            quests.OnAddedQuest += Quests_OnAddedQuest;
            quests.OnRemovedQuest += Quests_OnRemovedQuest;

            foreach (Quest quest in quests)
                OnQuestAddedToCollection(quest);
            this.picker = picker;
            this.quests = quests;
        }

        public ObservableCollection<ElementViewModel> Elements { get; }

        public ObservableCollection<ConnectionViewModel> Connections { get; }

        public IEnumerable<ElementViewModel> SelectedElements => Elements.Where(x => x.IsSelected);

        public IEnumerable<ConnectionViewModel> SelectedConnections => Connections.Where(x => x.IsSelected);

        public event Action<ConnectionViewModel> RequestNodePickerWindow = delegate { };

        private void Quests_OnRemovedQuest(QuestList arg1, Quest quest)
        {
            DeleteElement(questToViweModel[quest]);
            questToViweModel.Remove(quest);
        }

        private void Quests_OnAddedQuest(QuestList arg1, Quest quest)
        {
            OnQuestAddedToCollection(quest);
        }

        private void OnRequiredQuestAdded(Quest quest, Quest dependency)
        {
            if (!questToViweModel.ContainsKey(quest))
                throw new Exception("Fatal error - quest should be in list");

            if (!questToViweModel.ContainsKey(dependency))
                OnQuestAddedToCollection(dependency);

            ElementViewModel vmReq = questToViweModel[quest];
            ElementViewModel vmDep = questToViweModel[dependency];

            Connections.Add(new ConnectionViewModel(vmDep.OutputConnectors[0], vmReq.InputConnectors[0]));
        }

        private void OnQuestAddedToCollection(Quest quest)
        {
            if (questToViweModel.ContainsKey(quest))
                return;

            QuestViewModel vm = new(quest);
            AddElement(vm, useCurrentDragPoint ? currentDragPoint.X : 10000, useCurrentDragPoint ? currentDragPoint.Y : 10000);
            questToViweModel.Add(quest, vm);

            foreach (Quest dep in quest.RequiredQuests)
                OnRequiredQuestAdded(quest, dep);

            quest.RequiredQuests.CollectionChanged += (sender, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    foreach (Quest dependency in e.NewItems)
                        OnRequiredQuestAdded(quest, dependency);
                }
                else if (e.Action == NotifyCollectionChangedAction.Remove)
                {
                    foreach (Quest dependency in e.OldItems)
                    {
                        ElementViewModel depVm = questToViweModel[dependency];

                        ConnectionViewModel? connection = vm.InputConnectors[0]
                            .Connections.Where(t => t.From == depVm.OutputConnectors[0])
                            .FirstOrDefault();

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
            currentDragPoint = point;

            QuestDefinition quest = picker.ChooseQuest();
            if (quest == null)
                return;
            Quest q = new(quest.Id, quest.Title);
            useCurrentDragPoint = true;
            quests.AddQuest(q);
            useCurrentDragPoint = false;
        }

        public ConnectionViewModel OnConnectionDragStarted(ConnectorViewModel sourceConnector, Point currentDragPoint)
        {
            if (sourceConnector is OutputConnectorViewModel)
            {
                connectingTo = true;
                ConnectionViewModel connection = new((OutputConnectorViewModel) sourceConnector)
                {
                    ToPosition = currentDragPoint
                };

                Connections.Add(connection);

                return connection;
            }

            if (sourceConnector is InputConnectorViewModel)
            {
                connectingTo = false;
                ConnectionViewModel connection = new((InputConnectorViewModel) sourceConnector)
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

            Point pnt = nearbyConnector != null ? nearbyConnector.Position : currentDragPoint;

            if (connectingTo)
                connection.ToPosition = pnt;
            else
                connection.FromPosition = pnt;
        }

        public void OnConnectionDragCompleted(Point currentDragPoint,
            ConnectionViewModel newConnection,
            ConnectorViewModel sourceConnector)
        {
            QuestViewModel fromNewConnection = (QuestViewModel) newConnection.From?.Element;
            QuestViewModel toNewConnection = (QuestViewModel) newConnection.To?.Element;

            newConnection.Detach();
            Connections.Remove(newConnection);

            this.currentDragPoint = currentDragPoint;
            if (connectingTo)
            {
                InputConnectorViewModel nearbyConnector = FindNearbyInputConnector(newConnection, currentDragPoint);

                if (nearbyConnector == null)
                {
                    QuestDefinition quest = picker.ChooseQuest();
                    if (quest == null)
                        return;
                    Quest q = new(quest.Id, quest.Title);
                    useCurrentDragPoint = true;
                    bool added = quests.AddQuest(q);
                    useCurrentDragPoint = false;
                    if (added)
                        q.RequiredQuests.Add(fromNewConnection.Quest);
                    return;
                }

                if (nearbyConnector == null || sourceConnector.Element == nearbyConnector.Element)
                    return;

                Quest to = ((QuestViewModel) nearbyConnector.Element).Quest;
                Quest from = fromNewConnection.Quest;
                to.RequiredQuests.Add(from);
            }
            else
            {
                OutputConnectorViewModel nearbyConnector = FindNearbyOutputConnector(newConnection, currentDragPoint);

                if (nearbyConnector == null)
                {
                    QuestDefinition quest = picker.ChooseQuest();
                    if (quest == null)
                        return;
                    Quest q = new(quest.Id, quest.Title);
                    useCurrentDragPoint = true;
                    bool added = quests.AddQuest(q);
                    useCurrentDragPoint = false;
                    if (added)
                        toNewConnection.Quest.RequiredQuests.Add(q);
                    return;
                }

                if (nearbyConnector == null || sourceConnector.Element == nearbyConnector.Element)
                    return;

                Quest to = toNewConnection.Quest;
                Quest from = ((QuestViewModel) nearbyConnector.Element).Quest;
                to.RequiredQuests.Add(from);
            }
        }

        private InputConnectorViewModel FindNearbyInputConnector(ConnectionViewModel connection, Point mousePosition)
        {
            return Elements.SelectMany(x => x.InputConnectors).FirstOrDefault(x => AreClose(x.Position, mousePosition, 10));
        }

        private OutputConnectorViewModel FindNearbyOutputConnector(ConnectionViewModel connection, Point mousePosition)
        {
            return Elements.SelectMany(x => x.OutputConnectors).FirstOrDefault(x => AreClose(x.Position, mousePosition, 10));
        }

        private static bool AreClose(Point point1, Point point2, double distance)
        {
            return (point1 - point2).Length < distance;
        }

        private void DeleteElement(ElementViewModel element)
        {
            foreach (ConnectionViewModel item in element.AttachedConnections)
                Connections.Remove(item);
            Elements.Remove(element);
        }

        public void DeleteSelectedElements()
        {
            Elements.Where(x => x.IsSelected).Select(x => ((QuestViewModel) x).Quest).ToList().ForEach(x => quests.RemoveQuest(x));
        }

        public void DeleteSelectedConnections()
        {
            foreach (ConnectionViewModel connection in Connections.Where(x => x.IsSelected).ToList())
            {
                Quest from = ((QuestViewModel) connection.From.Element).Quest;
                Quest to = ((QuestViewModel) connection.To.Element).Quest;

                to.RequiredQuests.Remove(from);
            }
        }
    }
}