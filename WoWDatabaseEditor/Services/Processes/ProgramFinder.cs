using System;
using System.Collections.Generic;
using System.IO;
using WDE.Common.Services.Processes;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.Processes;

[AutoRegister]
[SingleInstance]
public class ProgramFinder : IProgramFinder
{
    public string? TryLocate(params string[] names)
    {
        List<string> paths = new();
        paths.AddIfNotNull(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
        paths.AddIfNotNull(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));
        paths.AddIfNotNull(Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator));

        foreach (var p in paths)
        {
            if (string.IsNullOrEmpty(p))
                continue;
            
            foreach (var name in names)
            {
                var path = Path.Combine(p, name);
                if (File.Exists(path))
                    return path;
            }
        }

        return null;
    }
}