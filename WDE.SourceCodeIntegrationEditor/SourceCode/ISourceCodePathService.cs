using System.Collections.Generic;
using WDE.Module.Attributes;

namespace WDE.SourceCodeIntegrationEditor.SourceCode;

[UniqueProvider]
public interface ISourceCodePathService
{
    IReadOnlyList<string> SourceCodePaths { get; }
}