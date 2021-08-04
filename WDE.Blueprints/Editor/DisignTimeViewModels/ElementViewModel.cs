using WDE.Blueprints.Enums;

namespace WDE.Blueprints.Editor.DisignTimeViewModels
{
    internal class ElementViewModel
    {
        public NodeType NodeType => NodeType.Statement;

        public string Name => "Node name";

        public bool IsSelected => true;

        public IEnumerable<Connector> InputConnectors =>
            new[]
            {
                new Connector("Input 1", IoType.Exec),
                new Connector("Input 2 (non empty)", IoType.Int, true),
                new Connector("Input 3")
            };

        public IEnumerable<Connector> OutputConnectors =>
            new[]
            {
                new Connector("Output 1 (non empty)", IoType.Exec, true),
                new Connector("Output 2 (non empty)", IoType.Int, true),
                new Connector("Output 3"),
                new Connector("Output 4"),
                new Connector("Output 5")
            };
    }


    internal class ConnectorViewModel : Connector
    {
        public ConnectorViewModel() : base("")
        {
        }
    }

    internal class Connector
    {
        public Connector(string name, IoType iotype = Enums.IoType.Int, bool nonEmpty = false)
        {
            Name = name;
            IoType = iotype;
            NonEmpty = nonEmpty;
            Color = Colors.DarkSeaGreen;
        }

        public string Name { get; }

        public bool NonEmpty { get; }

        public Color Color { get; }

        public IoType IoType { get; }
    }
}