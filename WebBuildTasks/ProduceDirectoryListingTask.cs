namespace WebBuildTasks;

using Microsoft.Build.Utilities;

public class ProduceDirectoryListingTask : Task
{
    public string Folder { get; set; } = "";
    public string BasePath { get; set; } = "";

    public override bool Execute()
    {
        if (!Directory.Exists(Path.Join(BasePath, Folder)))
        {
            Directory.CreateDirectory(Path.Join(BasePath, Folder));
        }
        
        List<string> files = new();
        foreach (string file in Directory.EnumerateFiles(Path.Join(BasePath, Folder), "*", SearchOption.AllDirectories))
        {
            files.Add(Path.GetRelativePath(BasePath, file));
        }

        File.WriteAllText(Path.Join(BasePath, Folder, "files.txt"), string.Join("\n", files));

        return true;
    }
}