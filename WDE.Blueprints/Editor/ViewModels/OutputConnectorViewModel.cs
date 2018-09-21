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
    public class OutputConnectorViewModel : ConnectorViewModel
    {
        public override ConnectorDirection ConnectorDirection => ConnectorDirection.Output;
        
        public ObservableCollection<ConnectionViewModel> Connections { get; }

        public OutputConnectorViewModel(ElementViewModel element, IOType iotype, string name, Color color)
            : base(element, iotype, name, color)
        {
            Connections = new ObservableCollection<ConnectionViewModel>();
            Connections.CollectionChanged += (e, w) =>
            {
                RaisePropertyChanged("NonEmpty");
            };
        }

        public override bool NonEmpty => Connections.Count > 0;
    }
}
