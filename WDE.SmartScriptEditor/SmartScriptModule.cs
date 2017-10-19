using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using Prism.Modularity;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Editor.ViewModels;
using WDE.SmartScriptEditor.Editor.Views;

namespace WDE.SmartScriptEditor
{
    public class SmartScriptModule : IModule
    {
        private readonly IUnityContainer _container;

        public SmartScriptModule(IUnityContainer container)
        {
            _container = container;
        }

        public void Initialize()
        {
            _container.RegisterType<ISolutionItemProvider, SmartScriptCreatureProvider>("Creature Script");
            _container.RegisterType<ISolutionItemProvider, SmartScriptGameobjectProvider>("Gameobject Script");
            _container.RegisterType<ISolutionItemProvider, SmartScriptQuestProvider>("Quest Script");
            _container.RegisterType<ISolutionItemProvider, SmartScriptAuraProvider>("Aura Script");
            _container.RegisterType<ISolutionItemProvider, SmartScriptSpellProvider>("Spell Script");
            _container.RegisterType<ISolutionItemProvider, SmartScriptTimedActionListProvider>("Timed action list Script");

            _container.Resolve<ISolutionEditorManager>().Register<SmartScriptSolutionItem>(item =>
            {
                var view = new SmartScriptEditorView();
                var solutionItem = item as SmartScriptSolutionItem;
                var vm = new SmartScriptEditorViewModel(solutionItem, _container, _container.Resolve<IHistoryManager>());
                view.DataContext = vm;

                DocumentEditor editor = new DocumentEditor();
                editor.Title = solutionItem.Name;
                editor.Content = view;
                editor.Undo = vm.UndoCommand;
                editor.Redo = vm.RedoCommand;
                editor.Save = vm.SaveCommand;

                return editor;
            });

            SmartDataLoader.Load(SmartDataManager.GetInstance(), new SmartDataFileLoader());
        }
    }
}
