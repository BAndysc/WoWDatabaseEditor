using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Solution;
using WDE.EventAiEditor;
using WDE.Module.Attributes;

namespace WDE.MangosEventAiEditor.Services
{
    /// <summary>Reports the creature_ai_scripts (EventAI) slot of a creature spawn.</summary>
    // parent scope: consumed cross-module (spawn editor bridge), the module scope is invisible there
    [AutoRegisterToParentScope]
    public class EventAiSpawnScriptSourceProvider : ISpawnScriptSourceProvider
    {
        private readonly ICurrentCoreVersion currentCoreVersion;
        private readonly IDatabaseProvider databaseProvider;

        public EventAiSpawnScriptSourceProvider(ICurrentCoreVersion currentCoreVersion,
            IDatabaseProvider databaseProvider)
        {
            this.currentCoreVersion = currentCoreVersion;
            this.databaseProvider = databaseProvider;
        }

        public async Task<IReadOnlyList<SpawnScriptSlot>> GetScripts(SpawnScriptOwner owner)
        {
            if (!owner.IsCreature || !currentCoreVersion.Current.EventAiFeatures.IsSupported)
                return Array.Empty<SpawnScriptSlot>();

            var lines = await databaseProvider.GetEventAi((int)owner.Entry);

            return new[]
            {
                new SpawnScriptSlot
                {
                    Name = "EventAI",
                    Detail = lines.Count > 0 ? $"{lines.Count} events" : null,
                    Exists = lines.Count > 0,
                    SolutionItem = new EventAiSolutionItem((int)owner.Entry),
                    Order = 0
                }
            };
        }
    }
}
