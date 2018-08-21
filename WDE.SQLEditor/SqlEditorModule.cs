using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using Prism.Commands;
using Prism.Modularity;
using WDE.Common.Managers;
using WDE.Common.Solution;
using WDE.SQLEditor.ViewModels;
using WDE.SQLEditor.Views;

namespace WDE.SQLEditor
{
    public class SqlEditorModule : IModule
    {
        private IUnityContainer _container;

        public SqlEditorModule(IUnityContainer container)
        {
            _container = container;
        }
        
        public void Initialize()
        {
            _container.Resolve<ISolutionEditorManager>().Register<MetaSolutionSQL>(item =>
            {
                var view = new SqlEditorView();
                var solutionItem = item as MetaSolutionSQL;
                var vm = new SqlEditorViewModel(solutionItem.ExportSql);
                view.DataContext = vm;

                DocumentEditor editor = new DocumentEditor();
                editor.Title = "Sql output";
                editor.Content = view;
                editor.CanClose = true;
                editor.Undo = new DelegateCommand(() => { }, ()=>false);
                editor.Redo = new DelegateCommand(() => { }, () => false);

                return editor;
            });
        }
    }
}
