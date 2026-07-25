using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using SponzaDemo;
using TheEngine;

var replayCaptures = args.Contains("--replay-captures");
args = args.Where(a => a != "--replay-captures").ToArray();

// the 91 MB of NeoDemo assets are too heavy to ship with the project, so they are
// looked up next to it: either a local (gitignored) Assets folder or the veldrid
// checkout, relative to the build output directory (bin/Debug/net8.0)
var candidates = args.Length > 0
    ? new[] { args[0] }
    : new[]
    {
        "Assets",
        "../../../Assets",
        "../../../../../../../../temp/veldrid/src/NeoDemo/Assets",
    };

var assetsPath = candidates.FirstOrDefault(Directory.Exists);
if (assetsPath == null)
{
    Console.WriteLine("NeoDemo assets not found. Looked in:");
    foreach (var candidate in candidates)
        Console.WriteLine($"  {Path.GetFullPath(candidate)}");
    Console.WriteLine("Pass the path to veldrid/src/NeoDemo/Assets as the first argument.");
    return;
}

var nativeWindowSettings = new NativeWindowSettings
{
    Size = new Vector2i(1280, 720),
    Title = "TheEngine - Sponza demo",
    // the Vulkan backend creates its own surface; GLFW must not create a GL context
    API = ContextAPI.NoAPI,
};

AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
{
    Console.WriteLine(eventArgs.ExceptionObject);
};

using (var window = new TheEngineVulkanOpenTkWindow(nativeWindowSettings, new SponzaGame(assetsPath, replayCaptures)))
    window.Run();
TheEngine.TheEngine.Deinit();
