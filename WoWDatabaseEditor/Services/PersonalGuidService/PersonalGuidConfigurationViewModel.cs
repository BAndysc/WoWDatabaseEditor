using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Windows.Input;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WoWDatabaseEditorCore.Services.PersonalGuidService;

[AutoRegister(Platforms.Desktop)]
public partial class PersonalGuidConfigurationViewModel : ObservableBase, IConfigurable
{
    private readonly IPersonalGuidRangeSettingsService service;
    private PersonalGuidData data;

    public PersonalGuidConfigurationViewModel(IPersonalGuidRangeSettingsService service)
    {
        this.service = service;
        data = service.CurrentData;
        isEnabled = data.Enabled;
        startCreatureGuid = data.StartCreature;
        currentCreatureGuid = data.CurrentCreature;
        creatureGuidCount = data.CreatureCount;
        startGameObjectGuid = data.StartGameObject;
        currentGameObjectGuid = data.CurrentGameObject;
        gameObjectGuidCount = data.GameObjectCount;

        var save = new DelegateCommand(() =>
        {
            data = new PersonalGuidData()
            {
                Enabled = isEnabled,
                StartCreature = startCreatureGuid,
                CurrentCreature = currentCreatureGuid,
                CreatureCount = creatureGuidCount,
                StartGameObject = startGameObjectGuid,
                CurrentGameObject = currentGameObjectGuid,
                GameObjectCount = gameObjectGuidCount
            };
            service.Override(data);
            RaisePropertyChanged(nameof(IsModified));
        });
        Save = save;
        ResetCreatureCounter = new DelegateCommand(() => CurrentCreatureGuid = startCreatureGuid);
        ResetGameObjectCounter = new DelegateCommand(() => CurrentGameObjectGuid = startGameObjectGuid);
        AutoDispose(this.ToObservable(() => StartCreatureGuid).Skip(1).SubscribeAction(_ => CurrentCreatureGuid = startCreatureGuid));
        AutoDispose(this.ToObservable(() => StartGameObjectGuid).Skip(1).SubscribeAction(_ => CurrentGameObjectGuid = startGameObjectGuid));
    }

    public ICommand ResetCreatureCounter { get; }
    public ICommand ResetGameObjectCounter { get; }
    
    [AlsoNotify(nameof(IsModified))] [Notify] private bool isEnabled;
    [AlsoNotify(nameof(IsModified), nameof(LastCreatureGuid))] [Notify] private uint startCreatureGuid;
    [AlsoNotify(nameof(IsModified), nameof(LastCreatureGuid))] [Notify] private uint creatureGuidCount;
    [AlsoNotify(nameof(IsModified))] [Notify] private uint currentCreatureGuid;
    public string LastCreatureGuid => creatureGuidCount == 0 ? "-" : ((long)startCreatureGuid + creatureGuidCount - 1).ToString();

    [AlsoNotify(nameof(IsModified), nameof(LastGameObjectGuid))] [Notify] private uint startGameObjectGuid;
    [AlsoNotify(nameof(IsModified), nameof(LastGameObjectGuid))] [Notify] private uint gameObjectGuidCount;
    [AlsoNotify(nameof(IsModified))] [Notify] private uint currentGameObjectGuid;
    public string LastGameObjectGuid => gameObjectGuidCount == 0 ? "-" : ((long)startGameObjectGuid + gameObjectGuidCount - 1).ToString();


    public ICommand Save { get; }
    public ImageUri Icon { get; } = new ImageUri("Icons/document_id_big.png");
    public string Name => "GUID range for spawns";
    public string? ShortDescription => "Use this to set the range of GUIDs for spawns";

    public bool IsModified =>
        isEnabled != data.Enabled ||
        startCreatureGuid != data.StartCreature ||
        creatureGuidCount != data.CreatureCount ||
        currentCreatureGuid != data.CurrentCreature ||
        startGameObjectGuid != data.StartGameObject ||
        gameObjectGuidCount != data.GameObjectCount ||
        currentGameObjectGuid != data.CurrentGameObject;

    public bool IsRestartRequired => false;
    public ConfigurableGroup Group => ConfigurableGroup.Advanced;
}