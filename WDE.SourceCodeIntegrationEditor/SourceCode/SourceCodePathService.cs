using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;
using WDE.Common.Services;
using WDE.Module.Attributes;
using WDE.SourceCodeIntegrationEditor.Settings;

namespace WDE.SourceCodeIntegrationEditor.SourceCode;

[AutoRegister]
[SingleInstance]
internal class SourceCodePathService : ISourceCodePathService, ISourceCodeConfiguration
{
    private readonly IUserSettings userSettings;

    public IReadOnlyList<string> SourceCodePaths { get; set; }

    public bool EnableVisualStudioIntegration { get; set; }

    public bool EnableRemoteVisualStudioConnection { get; set; }

    public string RemoteVisualStudioAddress { get; set; }

    public string RemoteVisualStudioKey { get; set; }

    public void Save()
    {
        userSettings.Update(new Data()
        {
            Paths = SourceCodePaths.ToList(),
            EnableVisualStudioIntegration = EnableVisualStudioIntegration,
            EnableRemoteVisualStudioConnection = EnableRemoteVisualStudioConnection,
            RemoteVisualStudioAddress = RemoteVisualStudioAddress,
            RemoteVisualStudioKey = RemoteVisualStudioKey
        });
    }

    public SourceCodePathService(IUserSettings userSettings)
    {
        this.userSettings = userSettings;
        var data = userSettings.Get<Data>();
        SourceCodePaths = data?.Paths ?? new List<string>();
        EnableVisualStudioIntegration = data?.EnableVisualStudioIntegration ?? true;
        EnableRemoteVisualStudioConnection = data?.EnableRemoteVisualStudioConnection ?? false;
        RemoteVisualStudioAddress = data?.RemoteVisualStudioAddress ?? "";
        RemoteVisualStudioKey = data?.RemoteVisualStudioKey ?? "";
    }

    private class Data : ISettings
    {
        public List<string> Paths { get; set; } = new();

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool EnableVisualStudioIntegration { get; set; } = true;

        public bool EnableRemoteVisualStudioConnection { get; set; }

        public string RemoteVisualStudioAddress { get; set; } = "";

        public string RemoteVisualStudioKey { get; set; } = "";
    }
}