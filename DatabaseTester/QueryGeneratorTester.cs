using System.Numerics;
using WDE.Common.Database;
using WDE.QueryGenerators.Base;
using WDE.QueryGenerators.Models;
using WDE.SqlQueryGenerator;

namespace DatabaseTester;

public class QueryGeneratorTester
{
    private readonly IQueryGenerator<CreatureSpawnModelEssentials> creature;
    private readonly IQueryGenerator<GameObjectSpawnModelEssentials> gameobject;
    private readonly IQueryGenerator<ISpawnGroupTemplate> spawnGroupTemplate;
    private readonly IQueryGenerator<ISpawnGroupSpawn> spawnGroupSpawn;
    private readonly IQueryGenerator<QuestChainDiff> questChain;
    private readonly IQueryGenerator<CreatureDiff> creatureDiff;
    private readonly IQueryGenerator<GameObjectDiff> gameobjectDiff;
    private readonly IQueryGenerator<ICreatureText> creatureText;
    private readonly IQueryGenerator<IGossipMenuOption> gossipMenuOption;
    private readonly IQueryGenerator<IGossipMenuLine> gossipMenu;
    private readonly IQueryGenerator<CreatureGossipUpdate> creatureTemplateDiff;
    private readonly IQueryGenerator<IPointOfInterest> pointOfInterest;
    private readonly IQueryGenerator<INpcTextFull> npcTextInsert;
    private readonly IQueryGenerator<INpcText> npcTextDelete;

    public QueryGeneratorTester(IQueryGenerator<CreatureSpawnModelEssentials> creature,
        IQueryGenerator<GameObjectSpawnModelEssentials> gameobject,
        IQueryGenerator<ISpawnGroupTemplate> spawnGroupTemplate,
        IQueryGenerator<ISpawnGroupSpawn> spawnGroupSpawn,
        IQueryGenerator<QuestChainDiff> questChain,
        IQueryGenerator<CreatureDiff> creatureDiff,
        IQueryGenerator<GameObjectDiff> gameobjectDiff,
        IQueryGenerator<ICreatureText> creatureText,
        IQueryGenerator<IGossipMenuOption> gossipMenuOption,
        IQueryGenerator<IGossipMenuLine> gossipMenu,
        IQueryGenerator<CreatureGossipUpdate> creatureTemplateDiff,
        IQueryGenerator<IPointOfInterest> pointOfInterest,
        IQueryGenerator<INpcTextFull> npcTextInsert,
        IQueryGenerator<INpcText> npcTextDelete)
    {
        this.creature = creature;
        this.gameobject = gameobject;
        this.spawnGroupTemplate = spawnGroupTemplate;
        this.spawnGroupSpawn = spawnGroupSpawn;
        this.questChain = questChain;
        this.creatureDiff = creatureDiff;
        this.gameobjectDiff = gameobjectDiff;
        this.creatureText = creatureText;
        this.gossipMenuOption = gossipMenuOption;
        this.gossipMenu = gossipMenu;
        this.creatureTemplateDiff = creatureTemplateDiff;
        this.pointOfInterest = pointOfInterest;
        this.npcTextInsert = npcTextInsert;
        this.npcTextDelete = npcTextDelete;
    }

    public IEnumerable<DatabaseTable?> Tables()
    {
        yield return creature.TableName;
        yield return gameobject.TableName;
        yield return spawnGroupTemplate.TableName;
        yield return spawnGroupSpawn.TableName;
        yield return creatureText.TableName;
        yield return gossipMenuOption.TableName;
        yield return gossipMenu.TableName;
        yield return creatureTemplateDiff.TableName;
        yield return pointOfInterest.TableName;
        yield return npcTextInsert.TableName;
    }

    public IEnumerable<Func<IQuery>> Generate()
    {
        yield return () => gossipMenuOption.Insert(new AbstractGossipMenuOption()
        {
            MenuId = 1,
        });
        yield return () => gossipMenuOption.Delete(new AbstractGossipMenuOption()
        {
            MenuId = 1
        });
        yield return () => gossipMenu.Insert(new AbstractGossipMenuLine()
        {
            MenuId = 1,
        });
        yield return () => gossipMenu.Delete(new AbstractGossipMenuLine()
        {
            MenuId = 1
        });
        yield return () => creature.Insert(new CreatureSpawnModelEssentials()
        {
            Guid = 0xFFFFFF - 1,
            Entry = 1
        });
        yield return () => creatureDiff.Update(new CreatureDiff()
        {
            Guid = 0xFFFFFF - 1,
            Position = Vector3.Zero,
            Orientation = 0
        });
        yield return () => gameobject.Insert(new GameObjectSpawnModelEssentials()
        {
            Guid = 0xFFFFFF - 1,
            Entry = 29
        });
        yield return () => creature.Delete(new CreatureSpawnModelEssentials()
        {
            Guid = 0xFFFFFF - 1
        });
        yield return () => gameobjectDiff.Update(new GameObjectDiff()
        {
            Guid = 0xFFFFFF - 1,
            Position = Vector3.Zero,
            Orientation = 0,
            Rotation = Quaternion.Identity
        });
        yield return () => gameobject.Delete(new GameObjectSpawnModelEssentials()
        {
            Guid = 0xFFFFFF - 1
        });
        yield return () => spawnGroupTemplate.Insert(new AbstractSpawnGroupTemplate()
        {
            Id = int.MaxValue - 1
        });
        yield return () => spawnGroupSpawn.Insert(new AbstractSpawnGroupSpawn()
        {
            TemplateId = int.MaxValue - 1,
            Guid = int.MaxValue - 1
        });
        yield return () => spawnGroupTemplate.Delete(new AbstractSpawnGroupTemplate()
        {
            Id = int.MaxValue - 1
        });
        yield return () => spawnGroupSpawn.Delete(new AbstractSpawnGroupSpawn()
        {
            TemplateId = int.MaxValue - 1,
            Guid = int.MaxValue - 1
        });
        yield return () => questChain.Update(new QuestChainDiff()
        {
            Id = int.MaxValue - 1,
            BreadcrumbQuestId = 1,
            ExclusiveGroup = -2,
            NextQuestId = 3,
            PrevQuestId = -1
        });
        yield return () => creatureText.Insert(new AbstractCreatureText()
        {
            CreatureId = 1,
            GroupId = byte.MaxValue - 1,
            Id = byte.MaxValue - 1,
            Text = "abc"
        });
        yield return () => creatureText.Delete(new AbstractCreatureText()
        {
            CreatureId = 1
        });
        yield return () => creatureTemplateDiff.Update(new CreatureGossipUpdate()
        {
            Entry = 1,
            GossipMenuId = 1
        });
        yield return () => pointOfInterest.Insert(new AbstractPointOfInterest()
        {
            Id = 0xFFFFFF - 1
        });
        yield return () => pointOfInterest.Delete(new AbstractPointOfInterest()
        {
            Id = 0xFFFFFF - 1
        });
        yield return () => npcTextInsert.Insert(new AbstractNpcTextFull()
        {
            Id = 0xFFFFFF - 10
        });
        yield return () => npcTextDelete.Delete(new AbstractNpcText()
        {
            Id = 0xFFFFFF - 10
        });
    }
}