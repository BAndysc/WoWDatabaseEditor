using WDE.Module.Attributes;
using WoWDatabaseEditorCore.Services;

namespace WoWDatabaseEditorCore.Avalonia.Services
{
    [AutoRegister]
    [SingleInstance]
    public class ProgramNameService : IProgramNameService
    {
        public string Title => Program.ApplicationName;
        public string Subtitle => "";
    }
}