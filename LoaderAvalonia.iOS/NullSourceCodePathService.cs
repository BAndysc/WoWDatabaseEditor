using System;
using System.Collections.Generic;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace LoaderAvalonia.iOS;

[AutoRegister]
[SingleInstance]
public class NullSourceCodePathService : ISourceCodePathService
{
    public IReadOnlyList<string> SourceCodePaths { get; set; } = Array.Empty<string>();
}