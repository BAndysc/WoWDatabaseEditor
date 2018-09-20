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

        public BlueprintEditorViewModel(BlueprintSolutionItem solutionItem)
        {
            this.solutionItem = solutionItem;
        }
    }
}
