using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Prism.Modularity;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Editor.ViewModels;
using WDE.SmartScriptEditor.Editor.Views;
using Prism.Ioc;
using Prism.Events;
using WDE.Common.Providers;
using WDE.Common.DBC;
using WDE.Common.Solution;
using WDE.SmartScriptEditor.Providers;

namespace WDE.SmartScriptEditor
{
    public class SmartScriptModule : IModule
    {
        public SmartScriptModule()
        {
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.Resolve<ISolutionEditorManager>().Register<SmartScriptSolutionItem>(item =>
            {
                var view = new SmartScriptEditorView();
                var solutionItem = item as SmartScriptSolutionItem;
                var vm = new SmartScriptEditorViewModel(solutionItem, containerProvider.Resolve<IHistoryManager>(), containerProvider.Resolve<IDatabaseProvider>(), containerProvider.Resolve<IEventAggregator>(), containerProvider.Resolve<ISmartFactory>(), containerProvider.Resolve<IItemFromListProvider>(), containerProvider.Resolve<SmartTypeListProvider>(), containerProvider.Resolve<ISolutionItemNameRegistry>());
                view.DataContext = vm;

                DocumentEditor editor = new DocumentEditor();
                editor.Title = containerProvider.Resolve<ISolutionItemNameRegistry>().GetName(solutionItem);
                editor.Content = view;
                editor.Undo = vm.UndoCommand;
                editor.Redo = vm.RedoCommand;
                editor.Save = vm.SaveCommand;
                editor.History = vm.History;
                editor.CanClose = true;

                return editor;
            });

            SmartDataLoader.Load(SmartDataManager.GetInstance(), new SmartDataFileLoader());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
        }
    }
}
