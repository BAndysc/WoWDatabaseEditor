using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace WDE.QuestChainEditor.Editor.DisignTimeViewModels
{
    internal class ElementViewModel
    {
        public string Name => "Node name";

        public uint Id => 25142;

        public bool IsSelected => true;

        public IEnumerable<Connector> InputConnectors => new[]
        {
            new Connector(""),
        };

        public IEnumerable<Connector> OutputConnectors => new[]
        {
            new Connector("", true),
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
