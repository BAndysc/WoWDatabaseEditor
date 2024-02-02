using WDE.Module.Attributes;

namespace WDE.Common.Debugging;

[UniqueProvider]
public interface IDebuggerInspectorService
{
    void OpenInspector();
}