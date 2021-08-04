namespace WDE.QuestChainEditor.Editor.ViewModels
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

        public event EventHandler OutputChanged;

        protected void AddInputConnector(string name, Color color)
        {
            InputConnectorViewModel inputConnector = new(this, name, color);
            InputConnectors.Add(inputConnector);
        }

        protected void AddOutputConnector(string name, Color color)
        {
            OutputConnectorViewModel outputConnector = new(this, name, color);
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