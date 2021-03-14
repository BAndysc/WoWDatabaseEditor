using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data
{
    [UniqueProvider]
    public interface IDbTableDefinitionProvider
    {
        DatabaseEditorTableDefinitionJson GetCreatureTemplateDefinition();
        DatabaseEditorTableDefinitionJson GetGameobjectTemplateDefinition();
        DatabaseEditorTableDefinitionJson GetCreatureLootTemplateDefinition();
    }
}