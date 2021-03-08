using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data
{
    [UniqueProvider]
    public interface IDbTableDataJsonProvider
    {
        string GetCreatureTemplateDefinitionJson();
        string GetGameobjectTemplateDefinitionJson();
    }
}