using Prism.Ioc;
using WDE.Common.Managers;
using WDE.Common.Solution;
using WDE.EventAiEditor;
using WDE.EventAiEditor.Editor.ViewModels;
using WDE.EventAiEditor.Models;
using WDE.Module.Attributes;

namespace WDE.MangosEventAiEditor.Providers
{
    [AutoRegisterToParentScope]
    public class EventAiEditorProvider : ISolutionItemEditorProvider<EventAiSolutionItem>
    {
        private readonly IContainerProvider containerProvider;

        public EventAiEditorProvider(IContainerProvider containerProvider)
        {
            this.containerProvider = containerProvider;
        }

        public IDocument GetEditor(EventAiSolutionItem item)
        {
            EventAiEditorViewModel vm = containerProvider.Resolve<EventAiEditorViewModel>((typeof(IEventAiSolutionItem), item));
            return vm;
        }
    }
}