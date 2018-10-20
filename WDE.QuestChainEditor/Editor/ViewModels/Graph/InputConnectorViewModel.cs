using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace WDE.QuestChainEditor.Editor.ViewModels
{
    public class InputConnectorViewModel : ConnectorViewModel
    {
        public override ConnectorDirection ConnectorDirection => ConnectorDirection.Input;
        
        public ObservableCollection<ConnectionViewModel> Connections { get; }

        public override bool NonEmpty => Connections.Count > 0;

        public InputConnectorViewModel(ElementViewModel element, string name, Color color)
            : base(element, name, color)
        {
            Connections = new ObservableCollection<ConnectionViewModel>();
            Connections.CollectionChanged += (e, w) =>
            {
                RaisePropertyChanged("NonEmpty");
            };
        }
    }
}
