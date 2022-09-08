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

    public QueryGeneratorTester(IQueryGenerator<CreatureSpawnModelEssentials> creature,
        IQueryGenerator<GameObjectSpawnModelEssentials> gameobject,
        IQueryGenerator<ISpawnGroupTemplate> spawnGroupTemplate,
        IQueryGenerator<ISpawnGroupSpawn> spawnGroupSpawn,
        IQueryGenerator<QuestChainDiff> questChain)
    {
        this.creature = creature;
        this.gameobject = gameobject;
        this.spawnGroupTemplate = spawnGroupTemplate;
        this.spawnGroupSpawn = spawnGroupSpawn;
        this.questChain = questChain;
    }

    public IEnumerable<string?> Tables()
    {
        yield return creature.TableName;
        yield return gameobject.TableName;
        yield return spawnGroupTemplate.TableName;
        yield return spawnGroupSpawn.TableName;
    }

    public IEnumerable<IQuery?> Generate()
    {
        yield return creature.Insert(new CreatureSpawnModelEssentials()
        {
            Guid = int.MaxValue - 1
        });
        yield return gameobject.Insert(new GameObjectSpawnModelEssentials()
        {
            Guid = int.MaxValue - 1
        });
        yield return creature.Delete(new CreatureSpawnModelEssentials()
        {
            Guid = int.MaxValue - 1
        });
        yield return gameobject.Delete(new GameObjectSpawnModelEssentials()
        {
            Guid = int.MaxValue - 1
        });
        yield return spawnGroupTemplate.Insert(new AbstractSpawnGroupTemplate()
        {
            Id = int.MaxValue - 1
        });
        yield return spawnGroupSpawn.Insert(new AbstractSpawnGroupSpawn()
        {
            TemplateId = int.MaxValue - 1,
            Guid = int.MaxValue - 1
        });
        yield return spawnGroupTemplate.Delete(new AbstractSpawnGroupTemplate()
        {
            Id = int.MaxValue - 1
        });
        yield return spawnGroupSpawn.Delete(new AbstractSpawnGroupSpawn()
        {
            TemplateId = int.MaxValue - 1,
            Guid = int.MaxValue - 1
        });
        yield return questChain.Update(new QuestChainDiff()
        {
            Id = int.MaxValue - 1,
            BreadcrumbQuestId = 1,
            ExclusiveGroup = -2,
            NextQuestId = 3,
            PrevQuestId = -1
        });
    }
}