using Prism.Ioc;
using Prism.Modularity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Blueprints.Editor.ViewModels;
using WDE.Blueprints.Editor.Views;
using WDE.Common.Managers;

namespace WDE.Blueprints
{
    public class BlueprintsModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.Resolve<ISolutionEditorManager>().Register<BlueprintSolutionItem>(item =>
            {
                var view = new BlueprintEditorView();
                var solutionItem = item as BlueprintSolutionItem;
                var vm = new BlueprintEditorViewModel(solutionItem);
                view.DataContext = vm;

                DocumentEditor editor = new DocumentEditor();
                editor.Title = "Blueprints";
                editor.Content = view;
                editor.CanClose = true;

                return editor;
            });
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
        }
    }
}
