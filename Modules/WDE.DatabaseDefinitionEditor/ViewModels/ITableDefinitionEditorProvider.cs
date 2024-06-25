using System;
using System.Collections.Generic;
using WDE.DatabaseEditors.Data.Structs;
using WDE.Module.Attributes;

namespace WDE.DatabaseDefinitionEditor.ViewModels;

[NonUniqueProvider]
public interface ITableDefinitionEditorProvider
{
    string Name { get; }
    bool IsValid { get; }
    int Order { get; }
    IEnumerable<DatabaseTableDefinitionJson> Definitions { get; }
    event Action? DefinitionsChanged;
}