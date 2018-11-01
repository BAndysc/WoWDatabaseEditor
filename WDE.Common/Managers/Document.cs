using System.Windows.Controls;
using System.Windows.Input;
using WDE.Common.History;

namespace WDE.Common.Managers
{
    public sealed class Document : IDocument
    {
        public string Title { get; set; }
        public ICommand Undo { get; set; }
        public ICommand Redo { get; set; }
        public ICommand Save { get; set; }

        public ICommand CloseCommand { get; set; }
        public bool CanClose { get; set; }
        
        public IHistoryManager History { get; set; }
        
        public ContentControl Content { get; set; }
    }
}