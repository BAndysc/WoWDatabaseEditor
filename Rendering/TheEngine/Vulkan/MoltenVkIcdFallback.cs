namespace TheEngine.Vulkan;

/// <summary>
/// macOS ships no system Vulkan driver: the loader (Silk.NET.Vulkan.Loader.Native) discovers
/// MoltenVK through ICD manifests that normally only a Vulkan SDK install provides. For machines
/// without the SDK we bundle MoltenVK (Silk.NET.MoltenVK.Native) and point the loader at its
/// manifest via VK_DRIVER_FILES - but only when no manifest is discoverable through the loader's
/// default search paths, so an installed SDK (newer MoltenVK, alternative drivers) always wins.
/// </summary>
public static class MoltenVkIcdFallback
{
    private static bool applied;

    /// <summary>Must run before the first loader call that scans drivers (instance extension
    /// enumeration / vkCreateInstance). Safe to call multiple times and on any OS.</summary>
    public static void EnsureVulkanDriverDiscoverable()
    {
        if (applied || !OperatingSystem.IsMacOS())
            return;
        applied = true;

        // an explicit driver override always wins
        if (Environment.GetEnvironmentVariable("VK_ICD_FILENAMES") != null ||
            Environment.GetEnvironmentVariable("VK_DRIVER_FILES") != null)
            return;

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        // the loader's default macOS manifest search directories
        string[] systemDirs =
        {
            Path.Combine(home, ".config/vulkan/icd.d"),
            "/etc/xdg/vulkan/icd.d",
            "/usr/local/etc/vulkan/icd.d",
            "/etc/vulkan/icd.d",
            Path.Combine(home, ".local/share/vulkan/icd.d"),
            "/usr/local/share/vulkan/icd.d",
            "/usr/share/vulkan/icd.d",
        };
        foreach (var dir in systemDirs)
        {
            try
            {
                if (Directory.Exists(dir) && Directory.EnumerateFiles(dir, "*.json").Any())
                    return; // a system driver manifest exists, let the loader use it
            }
            catch
            {
                // unreadable directory - treat as absent
            }
        }

        // runtimes/osx/native = framework-dependent layout, app root = published layout
        var baseDir = AppContext.BaseDirectory;
        foreach (var candidate in new[]
                 {
                     Path.Combine(baseDir, "runtimes", "osx", "native", "MoltenVK_icd.json"),
                     Path.Combine(baseDir, "MoltenVK_icd.json"),
                 })
        {
            if (File.Exists(candidate))
            {
                // .NET's SetEnvironmentVariable calls setenv() on unix, so the loader's getenv sees it
                Environment.SetEnvironmentVariable("VK_DRIVER_FILES", candidate);
                return;
            }
        }
    }
}
