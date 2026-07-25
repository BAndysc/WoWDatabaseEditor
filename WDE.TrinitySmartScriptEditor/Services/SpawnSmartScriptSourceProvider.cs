using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Solution;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor;

namespace WDE.TrinitySmartScriptEditor.Services
{
    /// <summary>
    /// Reports the smart_scripts slots of a spawn: the per-entry script and the per-guid override
    /// (negative entryorguid). Returns nothing on cores without smart scripts for that entity type.
    /// </summary>
    // parent scope: consumed cross-module (spawn editor bridge), the module scope is invisible there
    [AutoRegisterToParentScope]
    public class SpawnSmartScriptSourceProvider : ISpawnScriptSourceProvider
    {
        private readonly ICurrentCoreVersion currentCoreVersion;
        private readonly IDatabaseProvider databaseProvider;

        public SpawnSmartScriptSourceProvider(ICurrentCoreVersion currentCoreVersion,
            IDatabaseProvider databaseProvider)
        {
            this.currentCoreVersion = currentCoreVersion;
            this.databaseProvider = databaseProvider;
        }

        public async Task<IReadOnlyList<SpawnScriptSlot>> GetScripts(SpawnScriptOwner owner)
        {
            var type = owner.IsCreature ? SmartScriptType.Creature : SmartScriptType.GameObject;
            if (!currentCoreVersion.Current.SmartScriptFeatures.SupportedTypes.Contains(type))
                return Array.Empty<SpawnScriptSlot>();

            var entryScript = await databaseProvider.GetScriptForAsync(owner.Entry, (int)owner.Entry, type);
            var guidScript = await databaseProvider.GetScriptForAsync(owner.Entry, -(int)owner.Guid, type);

            return new[]
            {
                new SpawnScriptSlot
                {
                    Name = "Smart script",
                    Detail = entryScript.Count > 0 ? $"{entryScript.Count} lines" : null,
                    Exists = entryScript.Count > 0,
                    SolutionItem = new SmartScriptSolutionItem((int)owner.Entry, type),
                    Order = 0
                },
                new SpawnScriptSlot
                {
                    Name = "Smart script (guid)",
                    Detail = guidScript.Count > 0 ? $"{guidScript.Count} lines" : null,
                    Exists = guidScript.Count > 0,
                    SolutionItem = new SmartScriptSolutionItem(-(int)owner.Guid, type),
                    Order = 1
                }
            };
        }
    }
}
