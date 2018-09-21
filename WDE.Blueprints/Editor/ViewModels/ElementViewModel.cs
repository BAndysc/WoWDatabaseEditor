using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using WDE.Blueprints.Enums;

namespace WDE.Blueprints.Editor.ViewModels
{
    public abstract class ElementViewModel : BindableBase
    {
        public event EventHandler OutputChanged;

        public const double PreviewSize = 100;

        private double _x;
        public double X
        {
            get { return _x; }
            set { SetProperty(ref _x, value); }
        }

        private double _y;
        public double Y
        {
            get { return _y; }
            set { SetProperty(ref _y, value); }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

        private Color _color;
        public Brush Color => new SolidColorBrush(_color);

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set { SetProperty(ref _isSelected, value); }
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

        protected ElementViewModel(string name, Color color)
        {
            InputConnectors = new ObservableCollection<InputConnectorViewModel>();
            OutputConnectors = new ObservableCollection<OutputConnectorViewModel>();
            Name = name;
            _color = color;
        }

        protected void AddInputConnector(string name, IOType iotype, Color color)
        {
            var inputConnector = new InputConnectorViewModel(this, iotype, name, color);
            InputConnectors.Add(inputConnector);
        }

        protected void AddOutputConnector(string name, IOType iotype, Color color)
        {
            var outputConnector = new OutputConnectorViewModel(this, iotype, name, color);
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
