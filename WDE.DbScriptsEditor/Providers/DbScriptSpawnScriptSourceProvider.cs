using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Solution;
using WDE.DbScriptsEditor.Editor;
using WDE.DbScriptsEditor.Models;
using WDE.Module.Attributes;

namespace WDE.DbScriptsEditor.Providers
{
    /// <summary>
    /// Reports the dbscripts_on_* slots directly attached to a spawn: on-death for creatures,
    /// on-use (by entry and by guid) for gameobjects. Movement scripts are keyed by
    /// creature_movement ScriptId, not by the spawn, so they are not listed here.
    /// </summary>
    [AutoRegister]
    [SingleInstance]
    public class DbScriptSpawnScriptSourceProvider : ISpawnScriptSourceProvider
    {
        private readonly IDbScriptDatabaseProvider databaseProvider;

        public DbScriptSpawnScriptSourceProvider(IDbScriptDatabaseProvider databaseProvider)
        {
            this.databaseProvider = databaseProvider;
        }

        public async Task<IReadOnlyList<SpawnScriptSlot>> GetScripts(SpawnScriptOwner owner)
        {
            var slots = new List<SpawnScriptSlot>();
            if (owner.IsCreature)
                slots.Add(await Slot("On death", DbScriptType.CreatureDeath, owner.Entry, 10));
            else
            {
                slots.Add(await Slot("On use", DbScriptType.GoTemplateUse, owner.Entry, 10));
                slots.Add(await Slot("On use (guid)", DbScriptType.GoUse, owner.Guid, 11));
            }
            return slots;
        }

        public Task<ISolutionItem?> CreateMovementScriptItem(uint scriptId) =>
            Task.FromResult<ISolutionItem?>(new DbScriptSolutionItem(DbScriptType.CreatureMovement, scriptId));

        public async Task<uint?> SuggestFreeMovementScriptId()
        {
            var ids = await databaseProvider.GetScriptIds(DbScriptType.CreatureMovement);
            return ids.Count == 0 ? 1u : ids.Max() + 1;
        }

        private async Task<SpawnScriptSlot> Slot(string name, DbScriptType type, uint id, int order)
        {
            var steps = await databaseProvider.GetScript(type, id);
            return new SpawnScriptSlot
            {
                Name = name,
                Detail = steps.Count > 0 ? $"{steps.Count} steps" : null,
                Exists = steps.Count > 0,
                SolutionItem = new DbScriptSolutionItem(type, id),
                Order = order
            };
        }
    }
}
