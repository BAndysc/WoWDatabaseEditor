using System;
using WDE.Blueprints.Editor.ViewModels;
using WDE.Blueprints.Editor.Views;
using WDE.Common.Attributes;
using WDE.Common.Managers;
using WDE.Common.Solution;

namespace WDE.Blueprints.Providers
{
    [AutoRegister]
    public class BlueprintItemEditorProvider : ISolutionItemEditorProvider<BlueprintSolutionItem>
    {
        public DocumentEditor GetEditor(BlueprintSolutionItem item)
        {
            var view = new BlueprintEditorView();
            var vm = new BlueprintEditorViewModel(item);
            view.DataContext = vm;

            DocumentEditor editor = new DocumentEditor
            {
                Title = "Blueprints",
                Content = view,
                CanClose = true
            };

            return editor;
        }
    }
}
