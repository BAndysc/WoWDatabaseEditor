using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.Fallbacks;

[FallbackAutoRegister]
[SingleInstance]
public class FallbackSmartDefinitionEditorService : ISmartScriptDefinitionEditorService
{
    public bool IsSupported => false;

    public void EditEvent(int id) { }

    public void EditAction(int id) { }

    public void EditTarget(int id) { }

    public void Open() { }
}