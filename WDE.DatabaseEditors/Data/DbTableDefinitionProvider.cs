using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data
{
    [SingleInstance]
    [AutoRegister]
    public class DbTableDefinitionProvider : IDbTableDefinitionProvider
    {
        private readonly IDbTableDataSerializationProvider serializationProvider;
        private readonly IDbTableDataJsonProvider jsonProvider;

        private readonly DatabaseEditorTableDefinitionJson creatureTemplateDefinition;
        
        public DbTableDefinitionProvider(IDbTableDataSerializationProvider serializationProvider, IDbTableDataJsonProvider jsonProvider)
        {
            this.serializationProvider = serializationProvider;
            this.jsonProvider = jsonProvider;

            creatureTemplateDefinition =
                serializationProvider.DeserializeTableDefinition<DatabaseEditorTableDefinitionJson>(jsonProvider.GetCreatureTemplateDefinitionJson());
        }

        public DatabaseEditorTableDefinitionJson GetCreatureTemplateDefinition() => creatureTemplateDefinition;
    }
}