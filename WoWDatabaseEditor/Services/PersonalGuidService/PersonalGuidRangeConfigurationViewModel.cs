using System.Reactive.Linq;
using System.Windows.Input;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.Services;
using WDE.Common.Services.IdGenerator;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WoWDatabaseEditorCore.Services.PersonalGuidService;

/// <summary>
/// The "Personal guid range" panel for one guid kind, embedded in the "Id generation"
/// settings page (created via <see cref="IIdSource.CreateConfiguration"/>).
/// </summary>
public partial class PersonalGuidRangeConfigurationViewModel : ObservableBase, IIdSourceConfiguration
{
    private readonly IPersonalGuidRangeSettingsService service;
    private readonly GuidType type;

    public PersonalGuidRangeConfigurationViewModel(IPersonalGuidRangeSettingsService service, GuidType type)
    {
        this.service = service;
        this.type = type;

        var data = service.CurrentData;
        startGuid = type == GuidType.Creature ? data.StartCreature : data.StartGameObject;
        currentGuid = type == GuidType.Creature ? data.CurrentCreature : data.CurrentGameObject;
        guidCount = type == GuidType.Creature ? data.CreatureCount : data.GameObjectCount;

        ResetCounter = new DelegateCommand(() => CurrentGuid = startGuid);
        AutoDispose(this.ToObservable(() => StartGuid).Skip(1).SubscribeAction(_ => CurrentGuid = startGuid));
    }

    [Notify] [AlsoNotify(nameof(IsModified), nameof(LastGuid))] private uint startGuid;
    [Notify] [AlsoNotify(nameof(IsModified))] private uint currentGuid;
    [Notify] [AlsoNotify(nameof(IsModified), nameof(LastGuid))] private uint guidCount;

    public string LastGuid => guidCount == 0 ? "-" : ((long)startGuid + guidCount - 1).ToString();

    public ICommand ResetCounter { get; }

    public bool IsModified
    {
        get
        {
            var data = service.CurrentData;
            return type == GuidType.Creature
                ? startGuid != data.StartCreature || currentGuid != data.CurrentCreature || guidCount != data.CreatureCount
                : startGuid != data.StartGameObject || currentGuid != data.CurrentGameObject || guidCount != data.GameObjectCount;
        }
    }

    public void Apply()
    {
        var data = service.CurrentData;
        if (type == GuidType.Creature)
        {
            data.StartCreature = startGuid;
            data.CurrentCreature = currentGuid;
            data.CreatureCount = guidCount;
        }
        else
        {
            data.StartGameObject = startGuid;
            data.CurrentGameObject = currentGuid;
            data.GameObjectCount = guidCount;
        }
        data.Enabled = true; // configuring a range through the panel is opting in
        service.Override(data);
        RaisePropertyChanged(nameof(IsModified));
    }
}
