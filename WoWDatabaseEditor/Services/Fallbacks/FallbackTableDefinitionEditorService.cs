using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.Fallbacks;

[FallbackAutoRegister]
[SingleInstance]
public class FallbackTableDefinitionEditorService : ITableDefinitionEditorService
{
    public void EditDefinition(string path)
    {
    }

    public void Open()
    {
    }
}