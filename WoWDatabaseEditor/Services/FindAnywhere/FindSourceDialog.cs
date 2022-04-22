using System.Collections.Generic;
using System.Linq;

namespace WoWDatabaseEditorCore.Services.FindAnywhere;

public class FindSourceDialog
{
    public readonly IReadOnlyList<string> Parameters;
    public readonly string Name;

    public FindSourceDialog(string name, params string[] parameter)
    {
        Parameters = parameter.ToList();
        Name = name;
    }

    public override string ToString()
    {
        return Name;
    }
}

public class FindSourceDialogSpawnByEntry : FindSourceDialog
{
    public string EntryParameter { get; }
    public string SpawnParameter { get; }

    public FindSourceDialogSpawnByEntry(string name, string entryParameter, string spawnParameter) : base(name, new[]{entryParameter, spawnParameter})
    {
        EntryParameter = entryParameter;
        SpawnParameter = spawnParameter;
    }
}