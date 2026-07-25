using Prism.Ioc;
using WDE.Common.Managers;
using WDE.Common.Solution;
using WDE.DbScriptsEditor.Editor.ViewModels;
using WDE.Module.Attributes;

namespace WDE.DbScriptsEditor.Providers
{
    [AutoRegister]
    [SingleInstance]
    public class DbScriptEditorProvider : ISolutionItemEditorProvider<DbScriptSolutionItem>
    {
        private readonly IContainerProvider containerProvider;

        public DbScriptEditorProvider(IContainerProvider containerProvider)
        {
            this.containerProvider = containerProvider;
        }

        public IDocument GetEditor(DbScriptSolutionItem item)
        {
            return containerProvider.Resolve<DbScriptEditorViewModel>((typeof(DbScriptSolutionItem), item));
        }
    }
}
