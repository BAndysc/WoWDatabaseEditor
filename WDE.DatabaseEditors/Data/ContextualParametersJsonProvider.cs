using System.Collections.Generic;
using System.IO;
using System.Linq;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data;

[SingleInstance]
[AutoRegister]
public class ContextualParametersJsonProvider : IContextualParametersJsonProvider
{
    public IEnumerable<(string file, string content)> GetParameters()
    {
        return Directory.GetFiles("DatabaseContextualParameters/", "*.json").Select(f => (f, File.ReadAllText(f)));
    }
}