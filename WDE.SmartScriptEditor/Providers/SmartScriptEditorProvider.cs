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
using Prism.Ioc;

namespace WDE.SmartScriptEditor.Providers
{
    [AutoRegister]
    public class SmartScriptEditorProvider : ISolutionItemEditorProvider<SmartScriptSolutionItem>
    {
        private readonly ISolutionItemNameRegistry solutionItemNameRegistry;
        private readonly IContainerProvider containerProvider;

        public SmartScriptEditorProvider(ISolutionItemNameRegistry solutionItemNameRegistry, IContainerProvider containerProvider)
        {
            this.solutionItemNameRegistry = solutionItemNameRegistry;
            this.containerProvider = containerProvider;
        }

        public Document GetEditor(SmartScriptSolutionItem item)
        {
            var view = new SmartScriptEditorView();
            var vm = containerProvider.Resolve<SmartScriptEditorViewModel>();
            vm.SetSolutionItem(item);
            view.DataContext = vm;

            Document editor = new Document
            {
                Title = solutionItemNameRegistry.GetName(item),
                Content = view,
                Undo = vm.UndoCommand,
                Redo = vm.RedoCommand,
                Save = vm.SaveCommand,
                Copy = vm.CopyCommand,
                Paste = vm.PasteCommand,
                Cut = vm.CutCommand,
                History = vm.History,
                CanClose = true
            };
            editor.OnDispose += () => vm.Dispose();

            return editor;
        }
    }
}
