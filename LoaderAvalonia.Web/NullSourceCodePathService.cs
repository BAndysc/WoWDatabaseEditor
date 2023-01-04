using System;
using System.Collections.Generic;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace LoaderAvalonia.Web;

[AutoRegister]
[SingleInstance]
public class NullSourceCodePathService : ISourceCodePathService
{
    public IReadOnlyList<string> SourceCodePaths { get; set; } = Array.Empty<string>();
}