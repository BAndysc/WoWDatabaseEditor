using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.EventAiEditor.Acid;
using WDE.MangosEventAiEditor.Settings;
using WDE.Module.Attributes;

namespace WDE.MangosEventAiEditor.Acid
{
    [AutoRegisterToParentScope]
    [SingleInstance]
    public class AcidFileService : IAcidFileService
    {
        private readonly ICMangosSettingsProvider settings;
        private readonly ICurrentCoreVersion currentCoreVersion;

        public AcidFileService(ICMangosSettingsProvider settings,
            ICurrentCoreVersion currentCoreVersion)
        {
            this.settings = settings;
            this.currentCoreVersion = currentCoreVersion;
        }

        public bool IsEnabled => settings.UpdateAcidFileOnSave && !string.IsNullOrWhiteSpace(settings.DbRepositoryPath);

        public string? TryLocateAcidFile() => AcidFileLocator.Locate(settings.DbRepositoryPath, currentCoreVersion.Current.Tag);

        public async Task<string> SaveScriptAsync(long creatureEntry, string? creatureName, IReadOnlyList<IEventAiLine> lines)
        {
            var path = TryLocateAcidFile();
            if (path == null)
                throw new Exception($"Couldn't find the ACID .sql file in {settings.DbRepositoryPath}{Path.DirectorySeparatorChar}ACID{Path.DirectorySeparatorChar}. Check the CMaNGOS settings.");

            var bytes = await File.ReadAllBytesAsync(path);
            bool hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
            var content = Encoding.UTF8.GetString(bytes, hasBom ? 3 : 0, bytes.Length - (hasBom ? 3 : 0));

            var patched = AcidFilePatcher.UpdateCreatureScript(content, creatureEntry, creatureName, lines, out var error);
            if (patched == null)
                throw new Exception(error ?? "Unknown error while updating the ACID file");

            await File.WriteAllTextAsync(path, patched, new UTF8Encoding(hasBom));
            return path;
        }
    }
}
