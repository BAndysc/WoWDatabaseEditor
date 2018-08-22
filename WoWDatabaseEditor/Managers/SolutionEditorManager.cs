
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using WDE.Common;
using WDE.Common.Managers;
using WDE.Common.Windows;
using Prism.Ioc;

namespace WoWDatabaseEditor.Managers
{
    [WDE.Common.Attributes.AutoRegister]
    public class SolutionEditorManager : ISolutionEditorManager, IWindowProvider
    {
        private Dictionary<Type, Func<object, DocumentEditor>> _editors;
        private readonly Lazy<ISolutionExplorer> _solutionExplorer;

        public bool AllowMultiple => false;

        public string Name => "Solution explorer";

        public SolutionEditorManager(Lazy<ISolutionExplorer> solutionExplorer)
        {
            _editors = new Dictionary<Type, Func<object, DocumentEditor>>();
            _solutionExplorer = solutionExplorer;
        }

        public void Register<T>(Func<object, DocumentEditor> getter) where T : ISolutionItem
        {
            _editors.Add(typeof(T), getter);
        }

        public DocumentEditor GetEditor<T>(T item) where T : ISolutionItem
        {
            if (!_editors.ContainsKey(item.GetType()))
                return null;

            return _editors[item.GetType()](item);
        }

        public ContentControl GetView()
        {
            return _solutionExplorer.Value.GetSolutionExplorerView();
        }
    }
}
