using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Prism.Mvvm;

namespace WDE.Blueprints.Editor.ViewModels
{
    public class GraphViewModel : BindableBase
    {
        private bool connectingTo;
        private Point currentDragPoint;

        public ConnectionViewModel PendingConnection;

        public GraphViewModel()
        {
            Elements = new ObservableCollection<ElementViewModel>();
            Connections = new ObservableCollection<ConnectionViewModel>();
        }

        public ObservableCollection<ElementViewModel> Elements { get; }

        public ObservableCollection<ConnectionViewModel> Connections { get; }

        public IEnumerable<ElementViewModel> SelectedElements => Elements.Where(x => x.IsSelected);

        public IEnumerable<ConnectionViewModel> SelectedConnections => Connections.Where(x => x.IsSelected);

        public event Action<ConnectionViewModel> RequestNodePickerWindow = delegate { };

        public ElementViewModel AddElement(ElementViewModel model, double x, double y)
        {
            model.X = x;
            model.Y = y;
            Elements.Add(model);
            return model;
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

                if (((InputConnectorViewModel) sourceConnector).Connection != null)
                    Connections.Remove(((InputConnectorViewModel) sourceConnector).Connection);
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
            this.currentDragPoint = currentDragPoint;
            if (connectingTo)
            {
                InputConnectorViewModel nearbyConnector = FindNearbyInputConnector(newConnection, currentDragPoint);

                if (nearbyConnector == null)
                {
                    RequestNodePickerWindow(newConnection);
                    // pick
                    return;
                }

                if (nearbyConnector == null || sourceConnector.Element == nearbyConnector.Element)
                {
                    Connections.Remove(newConnection);
                    return;
                }

                ConnectionViewModel existingConnection = nearbyConnector.Connection;
                if (existingConnection != null)
                    Connections.Remove(existingConnection);

                newConnection.To = nearbyConnector;
            }
            else
            {
                OutputConnectorViewModel nearbyConnector = FindNearbyOutputConnector(newConnection, currentDragPoint);

                foreach (ConnectionViewModel con in Connections.Where(c => c.To == newConnection.To && c.From != null).ToList())
                    Connections.Remove(con);

                if (nearbyConnector == null)
                {
                    RequestNodePickerWindow(newConnection);
                    // pick
                    return;
                }

                if (nearbyConnector == null || sourceConnector.Element == nearbyConnector.Element)
                {
                    Connections.Remove(newConnection);
                    return;
                }

                newConnection.From = nearbyConnector;
            }
        }

        private InputConnectorViewModel FindNearbyInputConnector(ConnectionViewModel connection, Point mousePosition)
        {
            return Elements.SelectMany(x => x.InputConnectors)
                .Where(x => x.IoType == (connection.To == null ? connection.From.IoType : connection.To.IoType))
                .FirstOrDefault(x => AreClose(x.Position, mousePosition, 10));
        }

        private OutputConnectorViewModel FindNearbyOutputConnector(ConnectionViewModel connection, Point mousePosition)
        {
            return Elements.SelectMany(x => x.OutputConnectors)
                .Where(x => x.IoType == (connection.To == null ? connection.From.IoType : connection.To.IoType))
                .FirstOrDefault(x => AreClose(x.Position, mousePosition, 10));
        }

        private static bool AreClose(Point point1, Point point2, double distance)
        {
            return (point1 - point2).Length < distance;
        }

        public void DeleteElement(ElementViewModel element)
        {
            foreach (ConnectionViewModel item in element.AttachedConnections)
                Connections.Remove(item);
            Elements.Remove(element);
        }

        public void DeleteSelectedElements()
        {
            Elements.Where(x => x.IsSelected).ToList().ForEach(DeleteElement);
        }

        public void DeleteSelectedConnections()
        {
            foreach (ConnectionViewModel connection in Connections.Where(x => x.IsSelected).ToList())
            {
                connection.Detach();
                Connections.Remove(connection);
            }
        }
    }
}