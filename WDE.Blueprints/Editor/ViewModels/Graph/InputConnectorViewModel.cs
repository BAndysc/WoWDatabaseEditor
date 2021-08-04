using WDE.Blueprints.Enums;

namespace WDE.Blueprints.Editor.ViewModels
{
    public class InputConnectorViewModel : ConnectorViewModel
    {
        private ConnectionViewModel connection;

        public InputConnectorViewModel(ElementViewModel element, IoType iotype, string name, Color color) : base(element,
            iotype,
            name,
            color)
        {
        }

        public override ConnectorDirection ConnectorDirection => ConnectorDirection.Input;

        public ConnectionViewModel Connection
        {
            get => connection;
            set
            {
                if (connection != null && connection.From != null)
                    connection.From.Element.OutputChanged -= OnSourceElementOutputChanged;
                connection = value;
                if (connection != null && connection.From != null)
                    connection.From.Element.OutputChanged += OnSourceElementOutputChanged;
                RaiseSourceChanged();
                RaisePropertyChanged();
                RaisePropertyChanged("NonEmpty");
            }
        }

        public override bool NonEmpty => connection != null && connection.From != null;
        public event EventHandler SourceChanged;

        private void OnSourceElementOutputChanged(object sender, EventArgs e)
        {
            RaiseSourceChanged();
        }

        private void RaiseSourceChanged()
        {
            SourceChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}