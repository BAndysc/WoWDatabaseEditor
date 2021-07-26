using System;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Module.Attributes;
using WoWDatabaseEditorCore.ViewModels;

namespace WoWDatabaseEditorCore.Services.TextDocumentService
{
    [SingleInstance]
    [AutoRegister]
    public class TextDocumentService : ITextDocumentService
    {
        private readonly Func<TextDocumentViewModel> creator;

        public TextDocumentService(Func<TextDocumentViewModel> creator)
        {
            this.creator = creator;
        }

        public IDocument CreateDocument(string title, string text, string extenion)
        {
            var doc = creator();
            doc.Set(title, text, extenion);
            return doc;
        }
    }
}