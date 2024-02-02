using System.Collections.Generic;
using WDE.Module.Attributes;

namespace WDE.SourceCodeIntegrationEditor.Settings;

[UniqueProvider]
internal interface ISourceCodeConfiguration
{
    IReadOnlyList<string> SourceCodePaths { get; set; }
    bool EnableVisualStudioIntegration { get; set; }
    bool EnableRemoteVisualStudioConnection { get; set; }
    string RemoteVisualStudioAddress { get; set; }
    string RemoteVisualStudioKey { get; set; }
    void Save();
}