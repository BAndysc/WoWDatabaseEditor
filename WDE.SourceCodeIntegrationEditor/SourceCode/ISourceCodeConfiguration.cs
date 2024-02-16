using System.Collections.Generic;
using WDE.Module.Attributes;

namespace WDE.SourceCodeIntegrationEditor.SourceCode;

[UniqueProvider]
internal interface ISourceCodeConfiguration
{
    IReadOnlyList<string> SourceCodePaths { get; set; }
}