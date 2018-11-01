using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using WDE.Common.History;
using WDE.Common.Managers;

namespace WoWDatabaseEditor.Managers.ViewModels
{
    internal class DocumentDecorator : IDocument
    {
        private readonly IDocument document;
        private readonly Action<DocumentDecorator> closeAction;

        public string Title => document.Title;

        public ICommand Undo => document.Undo;

        public ICommand Redo => document.Redo;

        public ICommand Save => document.Save;

        public ICommand CloseCommand { get; private set; }

        public bool CanClose => document.CanClose;

        public IHistoryManager History => document.History;

        public ContentControl Content => document.Content;

        public System.Windows.Visibility Visibility { get; set; }

        public DocumentDecorator(IDocument document, Action<DocumentDecorator> closeAction)
        {
            this.document = document;
            this.closeAction = closeAction;
            CloseCommand = new DelegateCommand(Close);

            Visibility = System.Windows.Visibility.Visible;
        }

        private void Close()
        {
            closeAction(this);
            document.CloseCommand?.Execute(null);
        }
    }
}
