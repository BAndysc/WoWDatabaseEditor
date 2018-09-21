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
        public NodeViewModel(string name, int inputs, int outputs) : base(name, (Color)ColorConverter.ConvertFromString("#00A2E8"))
        {
            Name = name;
            
            nodeType = NodeType.Expression;

            for (int i = 0; i < inputs; ++i)
                AddInputConnector($"Input {i+1}", IOType.Int, Colors.DarkSeaGreen);
            for (int i = 0; i < outputs; ++i)

                AddOutputConnector($"Ooutput {i+1}", IOType.Int, Colors.DarkSeaGreen);
        }

        private NodeType nodeType;
        public override NodeType NodeType => nodeType;
    }
}
