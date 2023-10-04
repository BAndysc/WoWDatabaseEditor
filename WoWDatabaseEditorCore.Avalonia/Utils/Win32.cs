using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace WoWDatabaseEditorCore.Avalonia.Utils;

public static class Win32
{
    [SupportedOSPlatform("windows")]
    [DllImport( "kernel32.dll" )]
    public static extern bool AttachConsole(int dwProcessId);
    
    [SupportedOSPlatform("windows")]
    [DllImport("kernel32.dll")]
    public static extern bool AllocConsole();
    
    public const int ATTACH_PARENT_PROCESS = -1;
}