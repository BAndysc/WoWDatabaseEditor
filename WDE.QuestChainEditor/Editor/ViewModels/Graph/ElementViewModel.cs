using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace WDE.QuestChainEditor.Editor.ViewModels
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
        
        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set { SetProperty(ref _isSelected, value); }
        }
        
        public IList<InputConnectorViewModel> InputConnectors { get; }
        
        public IList<OutputConnectorViewModel> OutputConnectors { get; }

        public IEnumerable<ConnectionViewModel> AttachedConnections
        {
            get
            {
                return InputConnectors.SelectMany(x => x.Connections)
                    .Union(OutputConnectors.SelectMany(x => x.Connections))
                    .Where(x => x != null);
            }
        }

        protected ElementViewModel(string name)
        {
            InputConnectors = new ObservableCollection<InputConnectorViewModel>();
            OutputConnectors = new ObservableCollection<OutputConnectorViewModel>();
            Name = name;
        }

        protected void AddInputConnector(string name, Color color)
        {
            var inputConnector = new InputConnectorViewModel(this, name, color);
            InputConnectors.Add(inputConnector);
        }

        protected void AddOutputConnector(string name, Color color)
        {
            var outputConnector = new OutputConnectorViewModel(this, name, color);
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
