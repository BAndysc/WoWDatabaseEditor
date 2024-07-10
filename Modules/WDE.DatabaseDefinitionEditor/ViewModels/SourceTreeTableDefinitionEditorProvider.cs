using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.DatabaseEditors.Data.Structs;
using WDE.Module.Attributes;

namespace WDE.DatabaseDefinitionEditor.ViewModels;

[AutoRegister]
[SingleInstance]
public class SourceTreeTableDefinitionEditorProvider : ITableDefinitionEditorProvider
{
    private readonly ICurrentCoreVersion currentCoreVersion;

    public SourceTreeTableDefinitionEditorProvider(ICurrentCoreVersion currentCoreVersion)
    {
        this.currentCoreVersion = currentCoreVersion;
        var curDir = new DirectoryInfo(Environment.CurrentDirectory);
        IsValid = true;
        if (!TryAdd(curDir.Parent?.Parent?.Parent?.Parent))
            if (!TryAdd(curDir.Parent?.Parent?.Parent))
                IsValid = false;
    }

    private bool TryAdd(DirectoryInfo? root)
    {
        if (root == null)
            return false;

        var solutions = root.GetFiles("*.sln");
        if (!solutions.Any(solution => solution.Name.Contains("WoWDatabaseEditor")))
            return false;

        var files = root.GetDirectories("DbDefinitions", SearchOption.AllDirectories)
            .Where(dir => !AnyFolderOnPathToRootIsNamed(dir, "bin", "build"))
            .SelectMany(dir => dir.GetFiles("*.json", SearchOption.AllDirectories))
            .ToList();

        foreach (var file in files)
        {
            try
            {
                var definition = JsonConvert.DeserializeObject<DatabaseTableDefinitionJson>(File.ReadAllText(file.FullName));
                if (definition != null)
                {
                    if (!definition.Compatibility.Contains(currentCoreVersion.Current.Tag))
                        continue;
                    definition.AbsoluteFileName = file.FullName;
                    definition.FileName = Path.GetRelativePath(root.FullName, file.FullName);
                    definitions.Add(definition);
                }
            }
            catch (Exception e)
            {
                LOG.LogError(e);
            }
        }
        
        return true;
    }

    private bool AnyFolderOnPathToRootIsNamed(DirectoryInfo? dir, params string[] names)
    {
        while (dir != null)
        {
            if (names.Contains(dir.Name))
                return true;
            dir = dir.Parent;
        }

        return false;
    }

    private List<DatabaseTableDefinitionJson> definitions = new List<DatabaseTableDefinitionJson>();
    
    public int Order => 1;
    public string Name => "Sources";
    public bool IsValid { get; }
    public IEnumerable<DatabaseTableDefinitionJson> Definitions => definitions;
    public event Action? DefinitionsChanged;
}