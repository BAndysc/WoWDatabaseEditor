using System.Windows.Media;
using WDE.Blueprints.Enums;

namespace WDE.Blueprints.Editor.ViewModels
{
    public class NodeViewModel : ElementViewModel
    {
        public NodeViewModel(string name, NodeType node, int inputs, int outputs) : base(name)
        {
            Name = name;

            NodeType = node;

            for (var i = 0; i < inputs; ++i)
                AddInputConnector($"Input {i + 1}", i == 0 ? IoType.Exec : IoType.Int, Colors.DarkSeaGreen);
            for (var i = 0; i < outputs; ++i)

                AddOutputConnector($"Ooutput {i + 1}", i == 0 ? IoType.Exec : IoType.Int, Colors.DarkSeaGreen);
        }

        public override NodeType NodeType { get; }
    }
}