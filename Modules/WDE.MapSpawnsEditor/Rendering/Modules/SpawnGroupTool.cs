using WDE.Common.Database;
using WDE.Common.Services;
using WDE.MapRenderer.Managers;
using WDE.MapSpawnsEditor.Models;
using WDE.MapSpawnsEditor.Tools;
using WDE.MapSpawnsEditor.ViewModels;

namespace WDE.MapSpawnsEditor.Rendering.Modules;

public class SpawnGroupTool : IMapSpawnModule
{
    private readonly ISpawnGroupWizard spawnGroupWizard;
    private readonly ISpawnsContainer spawnsContainer;
    private readonly ZoneAreaManager zoneAreaManager;
    private readonly DbcManager dbcManager;

    private ISpawnGroupTemplate? lastSpawnGroup = null;

    public SpawnGroupTool(ISpawnGroupWizard spawnGroupWizard,
        ISpawnsContainer spawnsContainer,
        ZoneAreaManager zoneAreaManager,
        DbcManager dbcManager)
    {
        this.spawnGroupWizard = spawnGroupWizard;
        this.spawnsContainer = spawnsContainer;
        this.zoneAreaManager = zoneAreaManager;
        this.dbcManager = dbcManager;
    }
    
    public ISpawnGroupTemplate? LastSpawnGroup => lastSpawnGroup;
    
    public async Task CreateAndAssignSpawnGroup(SpawnInstance spawn)
    {
        lastSpawnGroup = await spawnGroupWizard.CreateSpawnGroup();
        await AssignSpawnGroup(spawn);
    }

    public async Task LeaveSpawnGroup(SpawnInstance spawn)
    {
        if (spawn.SpawnGroup == null)
            return;
        
        await spawnGroupWizard.LeaveSpawnGroup(spawn.SpawnGroup.Id, spawn.Entry, spawn.Guid, spawn is CreatureSpawnInstance ? SpawnGroupTemplateType.Creature : SpawnGroupTemplateType.GameObject);
        
        await spawnsContainer.Reload(spawn is CreatureSpawnInstance ? GuidType.Creature : GuidType.GameObject, spawn.Entry, spawn.Guid, zoneAreaManager, dbcManager);
    }
    
    public async Task AssignSpawnGroup(SpawnInstance spawn)
    {
        if (lastSpawnGroup == null)
            return;

        await spawnGroupWizard.AssignGuid(lastSpawnGroup.Id, spawn.Entry, spawn.Guid, spawn is CreatureSpawnInstance ? SpawnGroupTemplateType.Creature : SpawnGroupTemplateType.GameObject);
        
        await spawnsContainer.Reload(spawn is CreatureSpawnInstance ? GuidType.Creature : GuidType.GameObject, spawn.Entry, spawn.Guid, zoneAreaManager, dbcManager);
    }

    public async Task EditSpawnGroup(SpawnInstance spawn)
    {
        if (spawn.SpawnGroup == null)
            return;

        await spawnGroupWizard.EditSpawnGroup(spawn.SpawnGroup.Id);
    }

    public void MarkGroupAsLast(SpawnInstance spawn)
    {
        lastSpawnGroup = spawn.SpawnGroup;
    }
}