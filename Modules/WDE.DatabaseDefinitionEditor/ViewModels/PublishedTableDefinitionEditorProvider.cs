using System;
using System.Collections.Generic;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.Module.Attributes;

namespace WDE.DatabaseDefinitionEditor.ViewModels;

[AutoRegister]
[SingleInstance]
public class PublishedTableDefinitionEditorProvider : ITableDefinitionEditorProvider
{
    private readonly ITableDefinitionProvider definitionProvider;

    public PublishedTableDefinitionEditorProvider(ITableDefinitionProvider definitionProvider)
    {
        this.definitionProvider = definitionProvider;
        definitionProvider.DefinitionsChanged += () => DefinitionsChanged?.Invoke();
    }

    public string Name => "Build";
    public bool IsValid => true;
    public int Order => 2;
    public IEnumerable<DatabaseTableDefinitionJson> Definitions => definitionProvider.Definitions;
    public event Action? DefinitionsChanged;
}