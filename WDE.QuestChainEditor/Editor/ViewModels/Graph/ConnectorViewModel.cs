using System;
using System.Windows;
using System.Windows.Media;
using Prism.Mvvm;

namespace WDE.QuestChainEditor.Editor.ViewModels
{
    public abstract class ConnectorViewModel : BindableBase
    {
        private Point position;

        protected ConnectorViewModel(ElementViewModel element, string name, Color color)
        {
            Element = element;
            Name = name;
            Color = color;
        }

        public ElementViewModel Element { get; }

        public string Name { get; }

        public Color Color { get; }

        public abstract bool NonEmpty { get; }

        public Point Position
        {
            get => position;
            set
            {
                SetProperty(ref position, value);
                RaisePositionChanged();
            }
        }

        public abstract ConnectorDirection ConnectorDirection { get; }
        public event EventHandler PositionChanged;

        private void RaisePositionChanged()
        {
            PositionChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}