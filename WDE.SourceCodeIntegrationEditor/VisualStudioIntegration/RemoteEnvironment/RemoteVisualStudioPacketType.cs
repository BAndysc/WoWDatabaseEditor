namespace WDE.SourceCodeIntegrationEditor.VisualStudioIntegration.RemoteEnvironment;

internal enum RemoteVisualStudioPacketType : byte
{
    Auth = 1,
    Execute = 2,
    Reverse = 3,
    AuthOk = 4,
    FatalError = 5
}