using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace WDE.QuestChainEditor.Editor.ViewModels
{
    public abstract class ConnectorViewModel : BindableBase
    {
        public event EventHandler PositionChanged;
        
        public ElementViewModel Element { get; }
        
        public string Name { get; }
                
        public Color Color { get; }

        public abstract bool NonEmpty { get; }
        
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

        protected ConnectorViewModel(ElementViewModel element, string name, Color color)
        {
            Element = element;
            Name = name;
            Color = color;
        }

        private void RaisePositionChanged()
        {
            PositionChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
