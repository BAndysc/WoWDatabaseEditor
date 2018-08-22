using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Prism.Commands;
using Prism.Modularity;
using WDE.Common.Managers;
using WDE.Common.Solution;
using WDE.SQLEditor.ViewModels;
using WDE.SQLEditor.Views;
using Prism.Ioc;

namespace WDE.SQLEditor
{
    public class SqlEditorModule : IModule
    {        
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.Resolve<ISolutionEditorManager>().Register<MetaSolutionSQL>(item =>
            {
                var view = new SqlEditorView();
                var solutionItem = item as MetaSolutionSQL;
                var vm = new SqlEditorViewModel(solutionItem.ExportSql);
                view.DataContext = vm;

                DocumentEditor editor = new DocumentEditor();
                editor.Title = "Sql output";
                editor.Content = view;
                editor.CanClose = true;
                editor.Undo = new DelegateCommand(() => { }, () => false);
                editor.Redo = new DelegateCommand(() => { }, () => false);

                return editor;
            });
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
        }
    }
}
