using System;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Services;
using WDE.Common.Services.IdGenerator;
using WDE.Module.Attributes;
using WoWDatabaseEditorCore.Services.PersonalGuidService;

namespace WoWDatabaseEditorCore.Services.IdGeneratorService;

[AutoRegister]
[SingleInstance]
public class PersonalRangeCreatureGuidIdSource : IdSource<CreatureGuidIdType>
{
    private readonly IPersonalGuidRangeService guidRangeService;
    private readonly IPersonalGuidRangeSettingsService guidRangeSettings;

    public PersonalRangeCreatureGuidIdSource(IPersonalGuidRangeService guidRangeService,
        IPersonalGuidRangeSettingsService guidRangeSettings)
    {
        this.guidRangeService = guidRangeService;
        this.guidRangeSettings = guidRangeSettings;
    }

    public override string Name => "Personal guid range";
    public override bool IsConfigured => guidRangeService.IsConfigured;
    public override int DefaultPriority => 100; // preferred over take-max, but only once the range is configured

    public override IIdSourceConfiguration CreateConfiguration() =>
        new PersonalGuidRangeConfigurationViewModel(guidRangeSettings, GuidType.Creature);

    protected override Task<long> GetNext(CreatureGuidIdType request, int count, IdGenerationContext context)
    {
        try
        {
            return Task.FromResult((long)guidRangeService.GetNextGuidRange(GuidType.Creature, (uint)count));
        }
        catch (NoMoreGuidsException)
        {
            throw new NoMoreIdsException(typeof(CreatureGuidIdType));
        }
    }
}

[AutoRegister]
[SingleInstance]
public class PersonalRangeGameObjectGuidIdSource : IdSource<GameObjectGuidIdType>
{
    private readonly IPersonalGuidRangeService guidRangeService;
    private readonly IPersonalGuidRangeSettingsService guidRangeSettings;

    public PersonalRangeGameObjectGuidIdSource(IPersonalGuidRangeService guidRangeService,
        IPersonalGuidRangeSettingsService guidRangeSettings)
    {
        this.guidRangeService = guidRangeService;
        this.guidRangeSettings = guidRangeSettings;
    }

    public override string Name => "Personal guid range";
    public override bool IsConfigured => guidRangeService.IsConfigured;
    public override int DefaultPriority => 100;

    public override IIdSourceConfiguration CreateConfiguration() =>
        new PersonalGuidRangeConfigurationViewModel(guidRangeSettings, GuidType.GameObject);

    protected override Task<long> GetNext(GameObjectGuidIdType request, int count, IdGenerationContext context)
    {
        try
        {
            return Task.FromResult((long)guidRangeService.GetNextGuidRange(GuidType.GameObject, (uint)count));
        }
        catch (NoMoreGuidsException)
        {
            throw new NoMoreIdsException(typeof(GameObjectGuidIdType));
        }
    }
}

[AutoRegister]
[SingleInstance]
public class TakeMaxCreatureGuidIdSource : IdSource<CreatureGuidIdType>
{
    private readonly IDatabaseProvider databaseProvider;

    public TakeMaxCreatureGuidIdSource(IDatabaseProvider databaseProvider)
    {
        this.databaseProvider = databaseProvider;
    }

    public override string Name => "Max guid in the database + 1";

    protected override async Task<long> GetNext(CreatureGuidIdType request, int count, IdGenerationContext context)
    {
        var dbMax = await databaseProvider.GetMaxCreatureGuid();
        return Math.Max(dbMax, context.MaxUsedId ?? 0) + 1;
    }
}

[AutoRegister]
[SingleInstance]
public class TakeMaxGameObjectGuidIdSource : IdSource<GameObjectGuidIdType>
{
    private readonly IDatabaseProvider databaseProvider;

    public TakeMaxGameObjectGuidIdSource(IDatabaseProvider databaseProvider)
    {
        this.databaseProvider = databaseProvider;
    }

    public override string Name => "Max guid in the database + 1";

    protected override async Task<long> GetNext(GameObjectGuidIdType request, int count, IdGenerationContext context)
    {
        var dbMax = await databaseProvider.GetMaxGameObjectGuid();
        return Math.Max(dbMax, context.MaxUsedId ?? 0) + 1;
    }
}
