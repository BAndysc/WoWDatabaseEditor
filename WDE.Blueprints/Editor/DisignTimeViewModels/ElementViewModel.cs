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
            new Connector("Input 1", IOType.Exec),
            new Connector("Input 2 (non empty)", IOType.Int, true),
            new Connector("Input 3"),
        };

        public IEnumerable<Connector> OutputConnectors => new[]
        {
            new Connector("Output 1 (non empty)", IOType.Exec, true),
            new Connector("Output 2 (non empty)", IOType.Int, true),
            new Connector("Output 3"),
            new Connector("Output 4"),
            new Connector("Output 5")
        };
    }


    internal class ConnectorViewModel : Connector
    {
        public ConnectorViewModel() : base("", IOType.Int, false) { }
    }

    internal class Connector
    {
        public string Name { get; }

        public bool NonEmpty { get; }

        public Color Color { get; }

        public IOType IOType { get; }

        public Connector(string name, IOType iotype = IOType.Int, bool nonEmpty = false)
        {
            Name = name;
            IOType = iotype;
            NonEmpty = nonEmpty;
            Color = Colors.DarkSeaGreen;
        }
    }
}
