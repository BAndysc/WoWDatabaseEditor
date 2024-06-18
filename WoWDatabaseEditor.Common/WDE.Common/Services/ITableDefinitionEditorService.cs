using WDE.Module.Attributes;

namespace WDE.Common.Services;

[UniqueProvider]
public interface ITableDefinitionEditorService
{
    void EditDefinition(string path);
    void Open();
}