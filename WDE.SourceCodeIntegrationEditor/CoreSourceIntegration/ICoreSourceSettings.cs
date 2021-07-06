using WDE.Module.Attributes;

namespace WDE.SourceCodeIntegrationEditor.CoreSourceIntegration
{
    [UniqueProvider]
    public interface ICoreSourceSettings
    {
        bool SetCorePath(string? path);
        string? CurrentCorePath { get; }
    }
}