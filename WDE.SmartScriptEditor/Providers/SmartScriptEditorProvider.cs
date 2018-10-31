using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Module.Attributes;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Providers;
using WDE.Common.Solution;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Editor.ViewModels;
using WDE.SmartScriptEditor.Editor.Views;

namespace WDE.SmartScriptEditor.Providers
{
    [AutoRegister]
    public class SmartScriptEditorProvider : ISolutionItemEditorProvider<SmartScriptSolutionItem>
    {
        private readonly ISolutionItemNameRegistry solutionItemNameRegistry;
        private readonly IHistoryManager historyManager;
        private readonly IDatabaseProvider databaseProvider;
        private readonly IEventAggregator eventAggregator;
        private readonly ISmartDataManager smartDataManager;
        private readonly ISmartFactory smartFactory;
        private readonly IItemFromListProvider itemFromListProvider;
        private readonly SmartTypeListProvider smartTypeListProvider;

        public SmartScriptEditorProvider(
            ISolutionItemNameRegistry solutionItemNameRegistry,
            IHistoryManager historyManager,
            IDatabaseProvider databaseProvider,
            IEventAggregator eventAggregator,
            ISmartDataManager smartDataManager,
            ISmartFactory smartFactory,
            IItemFromListProvider itemFromListProvider,
            SmartTypeListProvider smartTypeListProvider         
            )
        {
            this.solutionItemNameRegistry = solutionItemNameRegistry;
            this.historyManager = historyManager;
            this.databaseProvider = databaseProvider;
            this.eventAggregator = eventAggregator;
            this.smartDataManager = smartDataManager;
            this.smartFactory = smartFactory;
            this.itemFromListProvider = itemFromListProvider;
            this.smartTypeListProvider = smartTypeListProvider;
        }

        public DocumentEditor GetEditor(SmartScriptSolutionItem item)
        {
            var view = new SmartScriptEditorView();
            var solutionItem = item as SmartScriptSolutionItem;
            var vm = new SmartScriptEditorViewModel(solutionItem, historyManager, databaseProvider, eventAggregator, smartDataManager, smartFactory, itemFromListProvider, smartTypeListProvider, solutionItemNameRegistry);
            view.DataContext = vm;

            DocumentEditor editor = new DocumentEditor
            {
                Title = solutionItemNameRegistry.GetName(solutionItem),
                Content = view,
                Undo = vm.UndoCommand,
                Redo = vm.RedoCommand,
                Save = vm.SaveCommand,
                History = vm.History,
                CanClose = true
            };

            return editor;
        }
    }
}
