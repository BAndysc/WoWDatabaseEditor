using System.Collections.Generic;
using WDE.SourceCodeIntegrationEditor.VisualStudioIntegration.DevelopmentToolsEnvironments;

namespace WDE.SourceCodeIntegrationEditor.VisualStudioIntegration.Services;

internal interface IVisualStudioProvider
{
    IEnumerable<IDTE> GetRunningInstances();
}