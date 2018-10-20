using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WDE.Blueprints.Editor.ViewModels
{
    public class GraphViewModel : BindableBase
    {
        public ObservableCollection<ElementViewModel> Elements { get; }
        
        public ObservableCollection<ConnectionViewModel> Connections { get; }

        public IEnumerable<ElementViewModel> SelectedElements => Elements.Where(x => x.IsSelected);

        public IEnumerable<ConnectionViewModel> SelectedConnections => Connections.Where(x => x.IsSelected);

        public event Action<ConnectionViewModel> RequestNodePickerWindow = delegate { };

        public GraphViewModel()
        {
            Elements = new ObservableCollection<ElementViewModel>();
            Connections = new ObservableCollection<ConnectionViewModel>();
        }

        public ElementViewModel AddElement(ElementViewModel model, double x, double y)
        {
            model.X = x;
            model.Y = y;
            Elements.Add(model);
            return model;
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

                if (((InputConnectorViewModel)sourceConnector).Connection != null)
                    Connections.Remove(((InputConnectorViewModel)sourceConnector).Connection);
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
        public void OnConnectionDragCompleted(Point currentDragPoint, ConnectionViewModel newConnection, ConnectorViewModel sourceConnector)
        {
            CurrentDragPoint = currentDragPoint;
            if (connectingTo)
            {
                var nearbyConnector = FindNearbyInputConnector(newConnection, currentDragPoint);

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

                var existingConnection = nearbyConnector.Connection;
                if (existingConnection != null)
                    Connections.Remove(existingConnection);

                newConnection.To = nearbyConnector;
            }
            else
            {
                var nearbyConnector = FindNearbyOutputConnector(newConnection, currentDragPoint);

                foreach (var con in Connections.Where(c => c.To == newConnection.To && c.From != null).ToList())
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
            return Elements.SelectMany(x => x.InputConnectors).Where(x => x.IOType == (connection.To == null ? connection.From.IOType : connection.To.IOType))
                .FirstOrDefault(x => AreClose(x.Position, mousePosition, 10));
        }

        private OutputConnectorViewModel FindNearbyOutputConnector(ConnectionViewModel connection, Point mousePosition)
        {
            return Elements.SelectMany(x => x.OutputConnectors).Where(x => x.IOType == (connection.To == null ? connection.From.IOType : connection.To.IOType))
                .FirstOrDefault(x => AreClose(x.Position, mousePosition, 10));
        }

        private static bool AreClose(Point point1, Point point2, double distance)
        {
            return (point1 - point2).Length < distance;
        }

        public void DeleteElement(ElementViewModel element)
        {
            foreach (var item in element.AttachedConnections)
                Connections.Remove(item);
            Elements.Remove(element);
        }

        public void DeleteSelectedElements()
        {
            Elements.Where(x => x.IsSelected)
                .ToList()
                .ForEach(DeleteElement);
        }

        public void DeleteSelectedConnections()
        {
            foreach (var connection in Connections.Where(x => x.IsSelected).ToList())
            {
                connection.Detach();
                Connections.Remove(connection);
            }
        }
        
    }
}
