using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using WDE.Common;
using WDE.Common.Managers;

namespace WoWDatabaseEditor.Managers
{
    public class SolutionEditorManager : ISolutionEditorManager
    {
        private Dictionary<Type, Func<object, DocumentEditor>> _editors;

        public SolutionEditorManager()
        {
            _editors = new Dictionary<Type, Func<object, DocumentEditor>>();
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
    }
}
