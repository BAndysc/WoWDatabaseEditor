using System.Collections.Generic;
using System.IO;
using WDE.Module.Attributes;

namespace WDE.SourceCodeIntegrationEditor.SourceCode.Openers;

[UniqueProvider]
internal interface IFileOpenerService
{
    bool TryOpen(FileInfo path, int lineNumber);
    IEnumerable<string> OpeningMethodNames { get; }
}