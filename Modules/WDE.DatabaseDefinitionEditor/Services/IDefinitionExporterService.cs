using WDE.DatabaseDefinitionEditor.ViewModels.DefinitionEditor;
using WDE.DatabaseEditors.Data.Structs;
using WDE.Module.Attributes;

namespace WDE.DatabaseDefinitionEditor.Services;

[UniqueProvider]
public interface IDefinitionExporterService
{
    DatabaseTableDefinitionJson Export(DefinitionViewModel vm);
}