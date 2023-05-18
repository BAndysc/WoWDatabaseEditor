using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Solution;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;
using WoWDatabaseEditorCore.ViewModels;

namespace WoWDatabaseEditorCore.Services.TextDocumentService
{
    [SingleInstance]
    [AutoRegister]
    public class TextDocumentService : ITextDocumentService
    {
        private readonly Func<TextDocumentViewModel> creator;
        private readonly IWindowManager windowManager;

        public TextDocumentService(Func<TextDocumentViewModel> creator, IWindowManager windowManager)
        {
            this.creator = creator;
            this.windowManager = windowManager;
        }

        public IDocument CreateDocument(string title, string text, string extension, bool inspectQuery = false)
        {
            var doc = creator();
            doc.Set(title, text, extension, inspectQuery);
            return doc;
        }
    }
}