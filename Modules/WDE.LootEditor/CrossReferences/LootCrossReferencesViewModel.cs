using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using DynamicData.Binding;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.MVVM;

namespace WDE.LootEditor.CrossReferences;

public partial class LootCrossReferencesViewModel : ObservableBase, IDialog
{
    private readonly LootSourceType type;
    private readonly LootEntry lootId;
    private readonly ICachedDatabaseProvider databaseProvider;
    private readonly ILootService lootService;
    private IParameter<long> spellParameter;
    private IParameter<long> itemsParameter;
    private IParameter<long> zoneAreaParameter;
    [Notify] private bool isLoading;

    public ObservableCollectionExtended<CrossReferenceViewModel> Items { get; } = new();
    
    public AsyncCommand<CrossReferenceViewModel> OpenCrossReferenceCommand { get; }
    
    public LootCrossReferencesViewModel(LootSourceType type, LootEntry lootId,
        ICachedDatabaseProvider databaseProvider,
        IParameterFactory parameterFactory,
        ILootService lootService)
    {
        this.type = type;
        this.lootId = lootId;
        this.databaseProvider = databaseProvider;
        this.lootService = lootService;
        itemsParameter = parameterFactory.Factory("ItemParameter");
        spellParameter = parameterFactory.Factory("SpellParameter");
        zoneAreaParameter = parameterFactory.Factory("ZoneAreaParameter");
        
        Title = $"Where is {type} loot {lootId} used?";
        Accept = new DelegateCommand(() =>
        {
            CloseOk?.Invoke();
        });
        Cancel = new DelegateCommand(() =>
        {
            CloseCancel?.Invoke();
        });
        OpenCrossReferenceCommand = new AsyncCommand<CrossReferenceViewModel>(async xRef =>
        {
            await lootService.EditLoot(xRef!.Type, xRef.Entry, xRef.DifficultyId);
        });
        isLoading = true;
        Load().ListenErrors();
    }

    private async Task Load()
    {
        switch (type)
        {
            case LootSourceType.Creature:
            {
                var loot = await databaseProvider.GetCreatureLootCrossReference((uint)lootId);
                AddCreatureLoot(LootSourceType.Creature, loot);
                break;
            }
            case LootSourceType.Skinning:
            {
                var loot = await databaseProvider.GetCreatureSkinningLootCrossReference((uint)lootId);
                AddCreatureLoot(LootSourceType.Skinning, loot);
                break;
            }
            case LootSourceType.Pickpocketing:
            {
                var loot = await databaseProvider.GetCreaturePickPocketLootCrossReference((uint)lootId);
                AddCreatureLoot(LootSourceType.Pickpocketing, loot);
                break;
            }
            case LootSourceType.GameObject:
            {
                var loot = await databaseProvider.GetGameObjectLootCrossReference((uint)lootId);
                foreach (var x in loot)
                {
                    Items.Add(new CrossReferenceViewModel(LootSourceType.GameObject, x.Entry, x.Name, 0));
                }
                break;
            }
            case LootSourceType.Reference:
            {
                var loot = await databaseProvider.GetReferenceLootCrossReference((uint)lootId);
                var names = await databaseProvider.GetLootTemplateName(LootSourceType.Reference);
                var namesDict = names.ToDictionary(x => x.Entry, x => x.Name);
                foreach (var x in loot)
                {
                    string? name = null;
                    switch (x.SourceType)
                    {
                        case LootSourceType.Reference:
                            namesDict.TryGetValue(x.Entry, out name);
                            Items.Add(new CrossReferenceViewModel(LootSourceType.Reference, x.Entry, name ?? "reference", 0));
                            break;
                        case LootSourceType.Creature:
                        {
                            var actualObjects = await databaseProvider.GetCreatureLootCrossReference(x.Entry);
                            AddCreatureLoot(LootSourceType.Creature, actualObjects);
                            break;
                        }
                        case LootSourceType.Skinning:
                        {
                            var actualObjects = await databaseProvider.GetCreatureSkinningLootCrossReference(x.Entry);
                            AddCreatureLoot(LootSourceType.Skinning, actualObjects);
                            break;
                        }
                        case LootSourceType.Pickpocketing:
                        {
                            var actualObjects = await databaseProvider.GetCreaturePickPocketLootCrossReference(x.Entry);
                            AddCreatureLoot(LootSourceType.Pickpocketing, actualObjects);
                            break;
                        }
                        case LootSourceType.GameObject:
                        {
                            var actualObjects = await databaseProvider.GetGameObjectLootCrossReference(x.Entry);
                            foreach (var obj in actualObjects)
                                Items.Add(new CrossReferenceViewModel(LootSourceType.GameObject, obj.Entry, obj.Name, 0));
                            break;
                        }
                        case LootSourceType.Item:
                            name = itemsParameter.ToString(x.Entry, ToStringOptions.WithoutNumber);
                            Items.Add(new CrossReferenceViewModel(LootSourceType.Item, x.Entry, name, 0));
                            break;
                        case LootSourceType.Spell:
                            name = spellParameter.ToString(x.Entry, ToStringOptions.WithoutNumber);
                            Items.Add(new CrossReferenceViewModel(LootSourceType.Spell, x.Entry, name, 0));
                            break;
                        case LootSourceType.Fishing:
                            name = zoneAreaParameter.ToString(x.Entry, ToStringOptions.WithoutNumber);
                            Items.Add(new CrossReferenceViewModel(LootSourceType.Fishing, x.Entry, name, 0));
                            break;
                        default:
                            Items.Add(new CrossReferenceViewModel(x.SourceType, x.Entry, "", 0));
                            break;
                    }
                }
                break;
            }
        }

        IsLoading = false;
    }

    private void AddCreatureLoot(LootSourceType type, (IReadOnlyList<ICreatureTemplate>, IReadOnlyList<ICreatureTemplateDifficulty>) loot)
    {
        foreach (var x in loot.Item1)
        {
            Items.Add(new CrossReferenceViewModel(type, x.Entry, x.Name, 0));
        }
        
        foreach (var x in loot.Item2)
        {
            var template = databaseProvider.GetCachedCreatureTemplate(x.Entry);
            Items.Add(new CrossReferenceViewModel(type, x.Entry, template?.Name ?? "(unknown creature)", x.DifficultyId));
        }
    }

    public int DesiredWidth => 460;
    public int DesiredHeight => 600;
    public string Title { get; }
    public bool Resizeable => true;
    public ICommand Accept { get; set; }
    public ICommand Cancel { get; set; }
    public event Action? CloseCancel;
    public event Action? CloseOk;
}

public class CrossReferenceViewModel
{
    public CrossReferenceViewModel(LootSourceType type, uint entry, string name, uint difficultyId)
    {
        Type = type;
        Entry = entry;
        Name = name;
        DifficultyId = difficultyId;
    }

    public LootSourceType Type { get; }
    public uint Entry { get; set; }
    public string Name { get; set; }
    public uint DifficultyId { get; }
}