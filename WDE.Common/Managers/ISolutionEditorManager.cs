using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WDE.Common.Managers
{
    public interface ISolutionEditorManager
    {
        void Register<T>(Func<object, DocumentEditor> getter) where T : ISolutionItem;
        DocumentEditor GetEditor<T>(T item) where T : ISolutionItem;
    }
}
