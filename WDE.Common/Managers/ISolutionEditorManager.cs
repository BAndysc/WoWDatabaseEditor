using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace WDE.Common.Managers
{
    public interface ISolutionEditorManager
    {
        void Register<T>(Func<object, DocumentEditor> getter) where T : ISolutionItem;
        DocumentEditor GetEditor<T>(T item) where T : ISolutionItem;
    }

    public class DocumentEditor : ContentControl
    {
        public string Title { get; set; }
        public ICommand Undo { get; set; }
        public ICommand Redo { get; set; }
        public ICommand Save { get; set; }
    }
}
