using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using WDE.Blueprints.Enums;

namespace WDE.Blueprints.Editor.DisignTimeViewModels
{
    internal class ElementViewModel
    {
        public NodeType NodeType => NodeType.Statement;

        public string Name => "Node name";

        public IEnumerable<Connector> InputConnectors => new[]
        {
            new Connector("Input 1"),
            new Connector("Input 2 (non empty)", true),
            new Connector("Input 3"),
        };

        public IEnumerable<Connector> OutputConnectors => new[]
        {
            new Connector("Output 1 (non empty)", true),
            new Connector("Output 2 (non empty)", true),
            new Connector("Output 3"),
            new Connector("Output 4"),
            new Connector("Output 5")
        };
    }


    internal class ConnectorViewModel : Connector
    {
        public ConnectorViewModel() : base("", false) { }
    }

    internal class Connector
    {
        public string Name { get; }

        public bool NonEmpty { get; }

        public Color Color { get; }

        public Connector(string name, bool nonEmpty = false)
        {
            Name = name;
            NonEmpty = nonEmpty;
            Color = Colors.DarkSeaGreen;
        }
    }
}
