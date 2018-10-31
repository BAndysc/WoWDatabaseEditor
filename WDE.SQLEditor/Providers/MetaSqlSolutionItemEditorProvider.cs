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

        public DocumentEditor GetEditor(MetaSolutionSQL item)
        {
            var view = new SqlEditorView();
            var vm = new SqlEditorViewModel(sqlGeneratorsRegistry.Value.GenerateSql(item as MetaSolutionSQL));
            view.DataContext = vm;

            DocumentEditor editor = new DocumentEditor();
            editor.Title = "Sql output";
            editor.Content = view;
            editor.CanClose = true;
            editor.Undo = new DelegateCommand(() => { }, () => false);
            editor.Redo = new DelegateCommand(() => { }, () => false);

            return editor;
        }
    }
}
