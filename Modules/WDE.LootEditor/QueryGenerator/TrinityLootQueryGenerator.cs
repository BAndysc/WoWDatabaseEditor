using System;
using System.Collections.Generic;
using System.Linq;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.LootEditor.Editor;
using WDE.LootEditor.Models;
using WDE.LootEditor.Utils;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.LootEditor.QueryGenerator;

public abstract class BaseTrinityLootQueryGenerator : ILootQueryGenerator
{
    private readonly IConditionQueryGenerator conditionQueryGenerator;
    private readonly ICurrentCoreVersion currentCoreVersion;
    private readonly ILootEditorFeatures editorFeatures;

    public BaseTrinityLootQueryGenerator(IConditionQueryGenerator conditionQueryGenerator,
        ICurrentCoreVersion currentCoreVersion,
        ILootEditorFeatures editorFeatures)
    {
        this.conditionQueryGenerator = conditionQueryGenerator;
        this.currentCoreVersion = currentCoreVersion;
        this.editorFeatures = editorFeatures;
    }
    
    public IQuery GenerateQuery(IReadOnlyList<LootGroupModel> models)
    {
        var transaction = Queries.BeginTransaction(DataDatabaseType.World);

        foreach (var group in models)
        {
            var type = group.Type;
            var conditionSourceType = editorFeatures.GetConditionSourceTypeFor(type);
            var tableName = editorFeatures.GetTableNameFor(type);
            var entry = (uint)group.Entry;
            
            transaction.Table(tableName)
                .Where(row => row.Column<uint>("Entry") == entry)
                .Delete();
            
            transaction.Add(conditionQueryGenerator.BuildDeleteQuery(new IDatabaseProvider.ConditionKey(conditionSourceType, (int)entry, null, null)));

            transaction.Table(tableName)
                .BulkInsert(group.Items.Select(CreateDatabaseObjectRow));

            var conditions = group.Items.SelectMany(g => g.Conditions
                    .Select(c => new AbstractConditionLine(conditionSourceType, (int)entry, g.Loot.ItemOrCurrencyId, 0, c)))
                .ToList();
            transaction.Add(conditionQueryGenerator.BuildInsertQuery(conditions));
            transaction.BlankLine();
        }

        return transaction.Close();
    }

    protected abstract object CreateDatabaseObjectRow(LootModel x);

    public IQuery GenerateUpdateLootIds(LootSourceType sourceType, uint solutionEntry, uint difficultyId, IReadOnlyList<LootEntry> rootLootEntries)
    {
        if (sourceType.SolutionEntryIsLootEntry() || !sourceType.CanUpdateSourceLootEntry())
            return Queries.Empty(DataDatabaseType.World);

        if (rootLootEntries.Count > editorFeatures.GetMaxLootEntryForType(sourceType, difficultyId))
            throw new Exception($"Too many root loot entries for {sourceType} (max {editorFeatures.GetMaxLootEntryForType(sourceType, difficultyId)})");

        var lootId = rootLootEntries.Count > 0 ? (uint)rootLootEntries[0] : 0;

        switch (sourceType)
        {
            case LootSourceType.Creature:
                if (difficultyId == 0)
                {
                    return Queries.Table(DatabaseTable.WorldTable("creature_template"))
                        .Where(row => row.Column<uint>("entry") == solutionEntry)
                        .Set("lootid", lootId)
                        .Update();                    
                }
                else
                {
                    return Queries.Table(DatabaseTable.WorldTable("creature_template_difficulty"))
                        .Where(row => row.Column<uint>("Entry") == solutionEntry &&
                                      row.Column<uint>("DifficultyID") == difficultyId)
                        .Set("LootID", lootId)
                        .Update();
                }
            case LootSourceType.GameObject:
                return Queries.Table(DatabaseTable.WorldTable("gameobject_template"))
                    .Where(row => row.Column<uint>("entry") == solutionEntry)
                    .Set("data1", lootId)
                    .Update();
            case LootSourceType.Skinning:
                if (difficultyId == 0)
                {
                    return Queries.Table(DatabaseTable.WorldTable("creature_template"))
                        .Where(row => row.Column<uint>("entry") == solutionEntry)
                        .Set("skinloot", lootId)
                        .Update();
                }
                else
                {
                    return Queries.Table(DatabaseTable.WorldTable("creature_template_difficulty"))
                        .Where(row => row.Column<uint>("Entry") == solutionEntry &&
                                      row.Column<uint>("DifficultyID") == difficultyId)
                        .Set("SkinLootID", lootId)
                        .Update();
                }
            case LootSourceType.Pickpocketing:
                if (difficultyId == 0)
                {
                    return Queries.Table(DatabaseTable.WorldTable("creature_template"))
                        .Where(row => row.Column<uint>("entry") == solutionEntry)
                        .Set("pickpocketloot", lootId)
                        .Update();
                }
                else
                {
                    return Queries.Table(DatabaseTable.WorldTable("creature_template_difficulty"))
                        .Where(row => row.Column<uint>("Entry") == solutionEntry &&
                                      row.Column<uint>("DifficultyID") == difficultyId)
                        .Set("PickPocketLootID", lootId)
                        .Update();
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(sourceType), sourceType, null);
        }
    }
}

[RequiresCore("TrinityMaster", "TrinityWrath", "Azeroth")]
[AutoRegister]
[SingleInstance]
public class TrinityLootQueryGenerator : BaseTrinityLootQueryGenerator
{
    public TrinityLootQueryGenerator(IConditionQueryGenerator conditionQueryGenerator, ICurrentCoreVersion currentCoreVersion, ILootEditorFeatures editorFeatures) : base(conditionQueryGenerator, currentCoreVersion, editorFeatures)
    {
    }

    protected override object CreateDatabaseObjectRow(LootModel x)
    {
        return new
        {
            Entry = x.Loot.Entry,
            Item = x.Loot.ItemOrCurrencyId,
            Reference = x.Loot.Reference,
            Chance = x.Loot.Chance,
            QuestRequired = x.Loot.QuestRequired,
            LootMode = x.Loot.LootMode,
            GroupId = x.Loot.GroupId,
            MinCount = x.Loot.MinCount,
            MaxCount = x.Loot.MaxCount,
            Comment = x.Loot.Comment
        };
    }
}

[RequiresCore("TrinityCata")]
[AutoRegister]
[SingleInstance]
public class TrinityCataLootQueryGenerator : BaseTrinityLootQueryGenerator
{
    public TrinityCataLootQueryGenerator(IConditionQueryGenerator conditionQueryGenerator, ICurrentCoreVersion currentCoreVersion, ILootEditorFeatures editorFeatures) : base(conditionQueryGenerator, currentCoreVersion, editorFeatures)
    {
    }

    protected override object CreateDatabaseObjectRow(LootModel x)
    {
        return new
        {
            Entry = x.Loot.Entry,
            Item = Math.Abs(x.Loot.ItemOrCurrencyId),
            IsCurrency = x.Loot.ItemOrCurrencyId < 0,
            Reference = x.Loot.Reference,
            Chance = x.Loot.Chance,
            QuestRequired = x.Loot.QuestRequired,
            LootMode = x.Loot.LootMode,
            GroupId = x.Loot.GroupId,
            MinCount = x.Loot.MinCount,
            MaxCount = x.Loot.MaxCount,
            Comment = x.Loot.Comment
        };
    }
}