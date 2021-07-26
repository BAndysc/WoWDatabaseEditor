using WDE.Common.Managers;
using WDE.Module.Attributes;

namespace WDE.Common.Services
{
    [UniqueProvider]
    public interface ITextDocumentService
    {
        IDocument CreateDocument(string title, string text, string extension);
    }
}