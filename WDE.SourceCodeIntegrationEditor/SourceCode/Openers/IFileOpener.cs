using System.IO;
using WDE.Module.Attributes;

namespace WDE.SourceCodeIntegrationEditor.SourceCode.Openers;

[NonUniqueProvider]
internal interface IFileOpener
{
    string Name { get; }
    int Order { get; }
    bool TryOpen(FileInfo path, int lineNumber);
}