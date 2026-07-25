using WDE.Common.CoreVersion;
using WDE.Module.Attributes;
using WoWDatabaseEditorCore.Services;

namespace WoWDatabaseEditorCore.Avalonia.Services
{
    [AutoRegister]
    [SingleInstance]
    public class ProgramNameService : IProgramNameService
    {
        private readonly ICurrentCoreVersion currentCoreVersion;

        public ProgramNameService(ICurrentCoreVersion currentCoreVersion)
        {
            this.currentCoreVersion = currentCoreVersion;
        }

        public string Title => $"{currentCoreVersion.Current.EditorTitle} {Program.ApplicationVersion}";
        public string Subtitle => "";
    }
}
