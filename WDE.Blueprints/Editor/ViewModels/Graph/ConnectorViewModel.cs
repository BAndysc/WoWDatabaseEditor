using WDE.Blueprints.Enums;

namespace WDE.Blueprints.Editor.ViewModels
{
    public abstract class ConnectorViewModel : BindableBase
    {
        private string directValue;

        private Point position;

        protected ConnectorViewModel(ElementViewModel element, IoType iotype, string name, Color color)
        {
            Element = element;
            Name = name;
            Color = color;
            IoType = iotype;

            if (iotype == Enums.IoType.Object)
                DirectValue = "me";
            else
                DirectValue = "0";
        }

        public ElementViewModel Element { get; }

        public string Name { get; }

        public IoType IoType { get; }

        public Color Color { get; }

        public abstract bool NonEmpty { get; }

        public string DirectValue
        {
            get => directValue;
            set => SetProperty(ref directValue, value);
        }

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