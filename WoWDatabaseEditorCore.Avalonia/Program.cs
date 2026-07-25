using System;

namespace WoWDatabaseEditorCore.Avalonia
{
    public class Program
    {
        public static Type[] PreloadedModules = new Type[]{};
        public static string ApplicationVersion = "2026.1";
        // static fallback used before the core version is known (the running app composes
        // the title from ICoreVersion.EditorTitle via IProgramNameService)
        public static string ApplicationName = "Database Editor " + ApplicationVersion;
    }
}
