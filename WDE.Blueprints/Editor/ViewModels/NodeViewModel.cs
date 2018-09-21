using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using WDE.Blueprints.Enums;

namespace WDE.Blueprints.Editor.ViewModels
{
    public class NodeViewModel : ElementViewModel
    {
        public NodeViewModel(string name, NodeType node, int inputs, int outputs) : base(name)
        {
            Name = name;

            nodeType = node;

            for (int i = 0; i < inputs; ++i)
                AddInputConnector($"Input {i+1}", IOType.Int, Colors.DarkSeaGreen);
            for (int i = 0; i < outputs; ++i)

                AddOutputConnector($"Ooutput {i+1}", IOType.Int, Colors.DarkSeaGreen);
        }

        private NodeType nodeType;
        public override NodeType NodeType => nodeType;
    }
}
