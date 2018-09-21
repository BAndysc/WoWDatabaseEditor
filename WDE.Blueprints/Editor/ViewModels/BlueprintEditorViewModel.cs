using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WDE.Blueprints.Editor.ViewModels
{
    class BlueprintEditorViewModel
    {
        private BlueprintSolutionItem solutionItem;

        public GraphViewModel GraphViewModel { get; }

        public BlueprintEditorViewModel(BlueprintSolutionItem solutionItem)
        {
            this.solutionItem = solutionItem;

            GraphViewModel = new GraphViewModel();

            GraphViewModel.AddElement(new NodeViewModel("Node 1", 0, 3), 10000, 10000);
            GraphViewModel.AddElement(new NodeViewModel("Node 2", 2, 3), 10100, 10000);
            GraphViewModel.AddElement(new NodeViewModel("Node 3", 1, 1), 10200, 10000);
            GraphViewModel.AddElement(new NodeViewModel("Node 4", 4, 1), 10300, 10000);
        }
    }
}
