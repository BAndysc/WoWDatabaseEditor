using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prism.Ioc;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.LootEditor.Picker.ViewModels;

[SingleInstance]
[AutoRegister]
public class LootPickerService : ILootPickerService
{
    private readonly IWindowManager windowManager;
    private readonly ICreatureEntryOrGuidProviderService creatureEntryOrGuidProviderService;
    private readonly IContainerProvider containerProvider;

    public LootPickerService(IWindowManager windowManager,
        ICreatureEntryOrGuidProviderService creatureEntryOrGuidProviderService,
        IContainerProvider containerProvider)
    {
        this.windowManager = windowManager;
        this.creatureEntryOrGuidProviderService = creatureEntryOrGuidProviderService;
        this.containerProvider = containerProvider;
    }

    public async Task<uint?> PickLoot(LootSourceType lootType)
    {
        if (lootType == LootSourceType.Creature)
        {
            // creature loot is so enormous that LootPickerViewModel can't be used without a modifications
            // (probably hide more than n items per creature)
            // for now, we will use a creature picker instead, as this is a rare case anyways
            var creature = await creatureEntryOrGuidProviderService.GetEntryFromService();
            if (creature == null)
                return null;
            return (uint)creature;
        }
        
        using var vm = containerProvider.Resolve<LootPickerViewModel>((typeof(LootSourceType), lootType));
        if (!await windowManager.ShowDialog(vm))
            return null;

        return vm.FocusedGroup?.LootEntry ?? vm.Items.FirstOrDefault()?.LootEntry;
    }

    public async Task<IReadOnlyList<uint>> PickLoots(LootSourceType lootType)
    {
        if (lootType == LootSourceType.Creature)
        {
            var creature = await creatureEntryOrGuidProviderService.GetEntriesFromService();
            return creature.Select(x => (uint)x).ToList();
        }
        var lootId = await PickLoot(lootType);
        if (lootId.HasValue)
            return new[]{lootId.Value};
        return Array.Empty<uint>();
    }
}