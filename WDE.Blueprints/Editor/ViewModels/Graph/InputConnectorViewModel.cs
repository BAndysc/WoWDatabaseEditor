using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using WDE.Blueprints.Enums;

namespace WDE.Blueprints.Editor.ViewModels
{
    public class InputConnectorViewModel : ConnectorViewModel
    {
        public event EventHandler SourceChanged;

        public override ConnectorDirection ConnectorDirection => ConnectorDirection.Input;

        private ConnectionViewModel _connection;
        public ConnectionViewModel Connection
        {
            get { return _connection; }
            set
            {
                if (_connection != null && _connection.From != null)
                    _connection.From.Element.OutputChanged -= OnSourceElementOutputChanged;
                _connection = value;
                if (_connection != null && _connection.From != null)
                    _connection.From.Element.OutputChanged += OnSourceElementOutputChanged;
                RaiseSourceChanged();
                RaisePropertyChanged();
                RaisePropertyChanged("NonEmpty");
            }
        }

        public override bool NonEmpty => _connection != null && _connection.From != null;

        private void OnSourceElementOutputChanged(object sender, EventArgs e)
        {
            RaiseSourceChanged();
        }

        public InputConnectorViewModel(ElementViewModel element, IOType iotype, string name, Color color)
            : base(element, iotype, name, color)
        {

        }

        private void RaiseSourceChanged()
        {
            SourceChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
