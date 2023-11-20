using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.MapSpawnsEditor.Models;
using WDE.MapSpawnsEditor.Tools.ViewModels;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;

namespace WDE.MapSpawnsEditor.Tools;

[AutoRegister]
[SingleInstance]
public class SpawnGroupWizard : ISpawnGroupWizard
{
    private readonly IWindowManager windowManager;
    private readonly Func<SpawnGroupPickerViewModel> spawnGroupPickerFactory;
    private readonly IPendingGameChangesService pendingGameChangesService;
    private readonly ITableEditorPickerService tableEditorPickerService;
    private readonly IQueryGenerator<ISpawnGroupTemplate> templateQueryGenerator;
    private readonly IQueryGenerator<ISpawnGroupSpawn> spawnQueryGenerator;

    public SpawnGroupWizard(IWindowManager windowManager,
        Func<SpawnGroupPickerViewModel> spawnGroupPickerFactory,
        IPendingGameChangesService pendingGameChangesService,
        ITableEditorPickerService tableEditorPickerService,
        IQueryGenerator<ISpawnGroupTemplate> templateQueryGenerator,
        IQueryGenerator<ISpawnGroupSpawn> spawnQueryGenerator)
    {
        this.windowManager = windowManager;
        this.spawnGroupPickerFactory = spawnGroupPickerFactory;
        this.pendingGameChangesService = pendingGameChangesService;
        this.tableEditorPickerService = tableEditorPickerService;
        this.templateQueryGenerator = templateQueryGenerator;
        this.spawnQueryGenerator = spawnQueryGenerator;
    }

    public async Task<ISpawnGroupTemplate?> CreateSpawnGroup()
    {
        var group = await ShowSpawnGroupDialog();
        if (!group.HasValue)
            return null;

        var query = templateQueryGenerator.Insert(new AbstractSpawnGroupTemplate()
        {
            Id = group.Value.id,
            Name = group.Value.name,
            Type = group.Value.type
        });

        if (query == null)
            throw new Exception("Failed to create a spawn group: couldn't generate query");
        
        pendingGameChangesService.AddGlobalQuery(query);
        await pendingGameChangesService.SaveAll();

        return new AbstractSpawnGroupTemplate()
        {
            Id = group.Value.id,
            Name = group.Value.name,
            Type = group.Value.type
        };
    }

    public async Task<bool> AssignGuid(uint template, uint entry, uint guid, SpawnGroupTemplateType type)
    {
        var query = spawnQueryGenerator.Insert(new AbstractSpawnGroupSpawn()
        {
            TemplateId = template,
            Guid = guid,
            Type = type
        });
        
        if (query == null)
            throw new Exception("Failed to assign a spawn group to the creature: couldn't generate query");
        
        pendingGameChangesService.AddQuery(type == SpawnGroupTemplateType.Creature ? GuidType.Creature : GuidType.GameObject, entry, guid, query);
        await pendingGameChangesService.SaveAll();

        return true;
    }

    public async Task<bool> LeaveSpawnGroup(uint template, uint entry, uint guid, SpawnGroupTemplateType type)
    {
        var query = spawnQueryGenerator.Delete(new AbstractSpawnGroupSpawn()
        {
            TemplateId = template,
            Guid = guid,
            Type = type
        });
        
        if (query == null)
            throw new Exception("Failed to leave a spawn group: couldn't generate query");

        pendingGameChangesService.AddQuery(type == SpawnGroupTemplateType.Creature ? GuidType.Creature : GuidType.GameObject, entry, guid, query);
        await pendingGameChangesService.SaveAll();

        return true;
    }

    public Task EditSpawnGroup(uint templateId)
    {
        if (templateQueryGenerator.TableName != null)
            return tableEditorPickerService.ShowForeignKey1To1(templateQueryGenerator.TableName.Value, new(templateId));
        return Task.CompletedTask;
    }

    private async Task<(uint id, string name, SpawnGroupTemplateType type)?> ShowSpawnGroupDialog()
    {
        var vm = spawnGroupPickerFactory();
        if (await windowManager.ShowDialog(vm))
        {
            return (vm.Id, vm.Name, vm.GroupType.Id);
        }
        else
            return null;
    }
}

[UniqueProvider]
public interface ISpawnGroupWizard
{
    Task<ISpawnGroupTemplate?> CreateSpawnGroup();
    Task<bool> AssignGuid(uint template, uint entry, uint guid, SpawnGroupTemplateType type);
    Task<bool> LeaveSpawnGroup(uint template, uint entry, uint guid, SpawnGroupTemplateType type);
    Task EditSpawnGroup(uint templateId);
}