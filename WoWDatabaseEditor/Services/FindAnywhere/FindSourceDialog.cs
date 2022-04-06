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