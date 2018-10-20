using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using WDE.Blueprints.Enums;

namespace WDE.Blueprints.Editor.ViewModels
{
    public abstract class ConnectorViewModel : BindableBase
    {
        public event EventHandler PositionChanged;
        
        public ElementViewModel Element { get; }
        
        public string Name { get; }
        
        public IOType IOType { get; }
        
        public Color Color { get; }

        public abstract bool NonEmpty { get; }

        private string _directValue;
        public string DirectValue
        {
            get { return _directValue; }
            set { SetProperty(ref _directValue, value); }
        }

        private Point _position;
        public Point Position
        {
            get { return _position; }
            set
            {
                SetProperty(ref _position, value);
                RaisePositionChanged();
            }
        }

        public abstract ConnectorDirection ConnectorDirection { get; }

        protected ConnectorViewModel(ElementViewModel element, IOType iotype, string name, Color color)
        {
            Element = element;
            Name = name;
            Color = color;
            IOType = iotype;

            if (iotype == IOType.Object)
                DirectValue = "me";
            else
                DirectValue = "0";
        }

        private void RaisePositionChanged()
        {
            PositionChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
