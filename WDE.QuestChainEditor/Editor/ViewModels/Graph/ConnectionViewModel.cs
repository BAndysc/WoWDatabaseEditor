using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WDE.QuestChainEditor.Editor.ViewModels
{
    public class ConnectionViewModel : BindableBase
    {
        private OutputConnectorViewModel _from;
        public OutputConnectorViewModel From
        {
            get { return _from; }
            set
            {
                if (_from != null)
                {
                    _from.PositionChanged -= OnFromPositionChanged;
                    _from.Connections.Remove(this);
                }

                _from = value;

                if (_from != null)
                {
                    _from.PositionChanged += OnFromPositionChanged;
                    _from.Connections.Add(this);
                    FromPosition = value.Position;
                    To = _to;
                }

                RaisePropertyChanged();
            }
        }

        private InputConnectorViewModel _to;
        public InputConnectorViewModel To
        {
            get { return _to; }
            set
            {
                if (_to != null)
                {
                    _to.PositionChanged -= OnToPositionChanged;
                    _to.Connections.Remove(this);
                }

                _to = value;

                if (_to != null)
                {
                    _to.PositionChanged += OnToPositionChanged;
                    _to.Connections.Add(this);
                    ToPosition = _to.Position;
                }

                RaisePropertyChanged();
            }
        }

        private Point _fromPosition;
        public Point FromPosition
        {
            get { return _fromPosition; }
            set
            {
                _fromPosition = value;
                RaisePropertyChanged();
            }
        }

        private Point _toPosition;
        public Point ToPosition
        {
            get { return _toPosition; }
            set
            {
                _toPosition = value;
                RaisePropertyChanged();
            }
        }

        private bool _isSelected;

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                RaisePropertyChanged();
            }
        }

        public ConnectionViewModel(OutputConnectorViewModel from, InputConnectorViewModel to)
        {
            From = from;
            To = to;
        }
        public ConnectionViewModel(InputConnectorViewModel to)
        {
            To = to;
        }
        public ConnectionViewModel(OutputConnectorViewModel from)
        {
            From = from;
        }

        public void Detach()
        {
            if (From != null)
                From.Connections.Remove(this);

            if (To != null)
                To.Connections.Remove(this);
        }

        private void OnFromPositionChanged(object sender, EventArgs e)
        {
            FromPosition = From.Position;
        }

        private void OnToPositionChanged(object sender, EventArgs e)
        {
            ToPosition = To.Position;
        }
    }
}
