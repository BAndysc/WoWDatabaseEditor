using System.Collections.Generic;
using System.Linq;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.SourceCodeIntegrationEditor.SourceCode;

[AutoRegister]
[SingleInstance]
internal class SourceCodePathService : ISourceCodePathService, ISourceCodeConfiguration
{
    private readonly IUserSettings userSettings;
    private IReadOnlyList<string> paths;

    public IReadOnlyList<string> SourceCodePaths
    {
        get => paths;
        set
        {
            paths = value;
            userSettings.Update(new Data() {Paths = value.ToList()});
        }
    }

    public SourceCodePathService(IUserSettings userSettings)
    {
        this.userSettings = userSettings;
        paths = userSettings.Get<Data>()?.Paths ?? new List<string>();
    }

    private class Data : ISettings
    {
        public List<string> Paths { get; set; } = new();
    }
}