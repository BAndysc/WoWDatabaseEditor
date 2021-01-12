using System.Collections.ObjectModel;
using System.Windows.Media;

namespace WDE.QuestChainEditor.Editor.ViewModels
{
    public class InputConnectorViewModel : ConnectorViewModel
    {
        public InputConnectorViewModel(ElementViewModel element, string name, Color color) : base(element, name, color)
        {
            Connections = new ObservableCollection<ConnectionViewModel>();
            Connections.CollectionChanged += (e, w) => { RaisePropertyChanged("NonEmpty"); };
        }

        public override ConnectorDirection ConnectorDirection => ConnectorDirection.Input;

        public ObservableCollection<ConnectionViewModel> Connections { get; }

        public override bool NonEmpty => Connections.Count > 0;
    }
}