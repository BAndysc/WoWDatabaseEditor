using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using Prism.Mvvm;
using WDE.Blueprints.Enums;

namespace WDE.Blueprints.Editor.ViewModels
{
    public abstract class ElementViewModel : BindableBase
    {
        public const double PreviewSize = 100;

        private bool isSelected;

        private string name;

        private double x;

        private double y;

        protected ElementViewModel(string name)
        {
            InputConnectors = new ObservableCollection<InputConnectorViewModel>();
            OutputConnectors = new ObservableCollection<OutputConnectorViewModel>();
            Name = name;
        }

        public double X
        {
            get => x;
            set => SetProperty(ref x, value);
        }

        public double Y
        {
            get => y;
            set => SetProperty(ref y, value);
        }

        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        public bool IsSelected
        {
            get => isSelected;
            set => SetProperty(ref isSelected, value);
        }

        public abstract NodeType NodeType { get; }

        public IList<InputConnectorViewModel> InputConnectors { get; }

        public IList<OutputConnectorViewModel> OutputConnectors { get; }

        public IEnumerable<ConnectionViewModel> AttachedConnections
        {
            get
            {
                return InputConnectors.Select(x => x.Connection)
                    .Union(OutputConnectors.SelectMany(x => x.Connections))
                    .Where(x => x != null);
            }
        }

        public event EventHandler OutputChanged;

        protected void AddInputConnector(string name, IoType iotype, Color color)
        {
            InputConnectorViewModel inputConnector = new(this, iotype, name, color);
            InputConnectors.Add(inputConnector);
        }

        protected void AddOutputConnector(string name, IoType iotype, Color color)
        {
            OutputConnectorViewModel outputConnector = new(this, iotype, name, color);
            OutputConnectors.Add(outputConnector);
        }

        //protected virtual void OnInputConnectorConnectionChanged()
        //{

        //}

        protected virtual void RaiseOutputChanged()
        {
            OutputChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}