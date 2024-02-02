using System;

namespace WDE.SourceCodeIntegrationEditor.VisualStudioIntegration.DevelopmentToolsEnvironments;

internal class CouldNotFetchDebugModeException : Exception
{
    public CouldNotFetchDebugModeException(string message) : base(message)
    {
    }
}