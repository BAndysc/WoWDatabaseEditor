﻿namespace WDE.QuestChainEditor.Editor.ViewModels
{
    public class OutputConnectorViewModel : ConnectorViewModel
    {
        public OutputConnectorViewModel(ElementViewModel element, string name, Color color) : base(element, name, color)
        {
            Connections = new ObservableCollection<ConnectionViewModel>();
            Connections.CollectionChanged += (e, w) => { RaisePropertyChanged("NonEmpty"); };
        }

        public override ConnectorDirection ConnectorDirection => ConnectorDirection.Output;

        public ObservableCollection<ConnectionViewModel> Connections { get; }

        public override bool NonEmpty => Connections.Count != 0;
    }
}