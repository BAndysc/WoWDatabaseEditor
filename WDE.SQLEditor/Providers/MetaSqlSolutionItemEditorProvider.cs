using Prism.Ioc;
using WDE.Common.Managers;
using WDE.Common.Solution;
using WDE.Module.Attributes;
using WDE.SQLEditor.ViewModels;

namespace WDE.SQLEditor.Providers
{
    [AutoRegister]
    public class MetaSqlSolutionItemEditorProvider : ISolutionItemEditorProvider<MetaSolutionSQL>
    {
        private readonly IContainerProvider containerProvider;

        public MetaSqlSolutionItemEditorProvider(IContainerProvider containerProvider)
        {
            this.containerProvider = containerProvider;
        }

        public IDocument GetEditor(MetaSolutionSQL item)
        {
            return containerProvider.Resolve<SqlEditorViewModel>((typeof(MetaSolutionSQL), item));
        }
    }
}