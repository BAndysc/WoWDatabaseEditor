using System.Collections.Generic;
using WDE.Module.Attributes;

namespace WDE.Common.Services;

[UniqueProvider]
public interface ISourceCodePathService
{
    IReadOnlyList<string> SourceCodePaths { get; }
}