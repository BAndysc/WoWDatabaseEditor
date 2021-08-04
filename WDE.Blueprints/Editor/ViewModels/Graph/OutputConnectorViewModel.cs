using WDE.Blueprints.Enums;

namespace WDE.Blueprints.Editor.ViewModels
{
    public class OutputConnectorViewModel : ConnectorViewModel
    {
        public OutputConnectorViewModel(ElementViewModel element, IoType iotype, string name, Color color) : base(element,
            iotype,
            name,
            color)
        {
            Connections = new ObservableCollection<ConnectionViewModel>();
            Connections.CollectionChanged += (e, w) => { RaisePropertyChanged("NonEmpty"); };
        }

        public override ConnectorDirection ConnectorDirection => ConnectorDirection.Output;

        public ObservableCollection<ConnectionViewModel> Connections { get; }

        public override bool NonEmpty => Connections.Count > 0;
    }
}