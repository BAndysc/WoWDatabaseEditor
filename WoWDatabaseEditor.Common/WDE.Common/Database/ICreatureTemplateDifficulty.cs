namespace WDE.Common.Database;

public interface ICreatureTemplateDifficulty
{ 
    uint Entry { get; }
    uint DifficultyId { get; }
    uint LootId { get; }
    uint SkinningLootId { get; }
    uint PickpocketLootId { get; }
}