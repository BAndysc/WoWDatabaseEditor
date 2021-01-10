using Prism.Commands;
using System;
using WDE.Module.Attributes;
using WDE.Common.Managers;
using WDE.Common.Solution;
using WDE.SQLEditor.ViewModels;
using WDE.SQLEditor.Views;

namespace WDE.SQLEditor.Providers
{
    [AutoRegister]
    public class MetaSqlSolutionItemEditorProvider : ISolutionItemEditorProvider<MetaSolutionSQL>
    {
        private readonly Lazy<ISolutionItemSqlGeneratorRegistry> sqlGeneratorsRegistry;

        public MetaSqlSolutionItemEditorProvider(Lazy<ISolutionItemSqlGeneratorRegistry> sqlGeneratorsRegistry)
        {
            this.sqlGeneratorsRegistry = sqlGeneratorsRegistry;
        }

        public IDocument GetEditor(MetaSolutionSQL item)
        {
            return new SqlEditorViewModel(sqlGeneratorsRegistry.Value.GenerateSql(item as MetaSolutionSQL));
        }
    }
}
