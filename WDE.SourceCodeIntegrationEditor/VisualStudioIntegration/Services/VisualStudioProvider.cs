using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WDE.Common.Services;
using WDE.Common.Tasks;
using WDE.Module.Attributes;
using WDE.SourceCodeIntegrationEditor.Settings;
using WDE.SourceCodeIntegrationEditor.VisualStudioIntegration.COM;
using WDE.SourceCodeIntegrationEditor.VisualStudioIntegration.DevelopmentToolsEnvironments;
using WDE.SourceCodeIntegrationEditor.VisualStudioIntegration.RemoteEnvironment;

namespace WDE.SourceCodeIntegrationEditor.VisualStudioIntegration.Services;

[AutoRegister]
[SingleInstance]
internal class VisualStudioProvider : IVisualStudioProvider
{
    private readonly ISourceCodeConfiguration configuration;
    private readonly ISourceCodePathService sourceCodePathService;
    private readonly IMainThread mainThread;

    public VisualStudioProvider(ISourceCodeConfiguration configuration,
        ISourceCodePathService sourceCodePathService,
        IMainThread mainThread)
    {
        this.configuration = configuration;
        this.sourceCodePathService = sourceCodePathService;
        this.mainThread = mainThread;
    }

    public IEnumerable<IDTE> GetRunningInstances()
    {
        if (!configuration.EnableVisualStudioIntegration)
            yield break;

        if (OperatingSystem.IsWindows())
        {
            foreach (var x in VisualStudioHelper
                         .GetVisualStudioInstances()
                         .Select(x => new LocalVisualStudioEnv(mainThread, x)))
                yield return x;
        }

        if (configuration.EnableRemoteVisualStudioConnection)
        {
            var indexOfColon = configuration.RemoteVisualStudioAddress.IndexOf(':');

            if (indexOfColon == -1)
                yield break;

            var address = configuration.RemoteVisualStudioAddress.Substring(0, indexOfColon);
            if (!int.TryParse(configuration.RemoteVisualStudioAddress.Substring(indexOfColon + 1), out var port))
                yield break;

            yield return new RemoteVisualStudioEnv(mainThread,
                address,
                port,
                configuration.RemoteVisualStudioKey);
        }
    }
}