using System;
using System.Collections.Generic;
using System.IO;

namespace CrashReport;

public class ProgramFinder
{
    private string? TryLocateCore(bool includeCurDir, string[] names)
    {
        List<string?> paths = new();
        paths.Add(Environment.CurrentDirectory);
        paths.Add(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
        paths.Add(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));
        if (Environment.GetEnvironmentVariable("PATH") is { } envPath)
            paths.AddRange(envPath.Split(Path.PathSeparator));

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
    
    public string? TryLocate(params string[] names)
    {
        return TryLocateCore(false, names);
    }

    public string? TryLocateIncludingCurrentDir(params string[] names)
    {
        return TryLocateCore(true, names);
    }
}