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

[AutoRegister]
[SingleInstance]
[RequiresCore("CMaNGOS-TBC", "CMaNGOS-Classic", "CMaNGOS-WoTLK")]
public class CmangosLootQueryGenerator : ILootQueryGenerator
{
    private readonly ICurrentCoreVersion currentCoreVersion;
    private readonly ILootEditorFeatures editorFeatures;

    public CmangosLootQueryGenerator(ICurrentCoreVersion currentCoreVersion,
        ILootEditorFeatures editorFeatures)
    {
        this.currentCoreVersion = currentCoreVersion;
        this.editorFeatures = editorFeatures;
    }
    
    public IQuery GenerateQuery(IReadOnlyList<LootGroupModel> models)
    {
        var transaction = Queries.BeginTransaction(DataDatabaseType.World);

        foreach (var group in models)
        {
            var type = group.Type;
            var tableName = editorFeatures.GetTableNameFor(type);
            var entry = (int)(uint)group.Entry;
            var name = group.Name;
            
            transaction.Table(tableName)
                .Where(row => row.Column<uint>("entry") == entry)
                .Delete();

            if (type == LootSourceType.Reference)
            {
                transaction.Table(DatabaseTable.WorldTable("reference_loot_template_names"))
                    .Where(row => row.Column<uint>("entry") == entry)
                    .Delete();

                if (!string.IsNullOrWhiteSpace(name))
                {
                    transaction.Table(DatabaseTable.WorldTable("reference_loot_template_names"))
                        .Insert(new
                        {
                            entry = entry,
                            name = name
                        });
                }
            }

            if (editorFeatures.HasCommentField(type))
            {
                transaction.Table(tableName)
                    .BulkInsert(group.Items.Select(x => new
                    {
                        entry = x.Loot.Entry,
                        item = x.Loot.ItemOrCurrencyId,
                        ChanceOrQuestChance = (x.Loot.QuestRequired ? -1 : 1) * x.Loot.Chance,
                        groupid = x.Loot.GroupId,
                        mincountOrRef = x.Loot.IsReference() ? -(int)x.Loot.Reference : x.Loot.MinCount,
                        maxcount = x.Loot.MaxCount,
                        condition_id = 0,
                        comments = x.Loot.Comment
                    }));
            }
            else
            {
                transaction.Table(tableName)
                    .BulkInsert(group.Items.Select(x => new
                    {
                        entry = x.Loot.Entry,
                        item = x.Loot.ItemOrCurrencyId,
                        ChanceOrQuestChance = (x.Loot.QuestRequired ? -1 : 1) * x.Loot.Chance,
                        groupid = x.Loot.GroupId,
                        mincountOrRef = x.Loot.IsReference() ? -(int)x.Loot.Reference : x.Loot.MinCount,
                        maxcount = x.Loot.MaxCount,
                        condition_id = 0
                    }));
            }

            transaction.BlankLine();
        }

        return transaction.Close();
    }

    public IQuery GenerateUpdateLootIds(LootSourceType sourceType, uint solutionEntry, uint difficultyId, IReadOnlyList<LootEntry> rootLootEntries)
    {
        if (sourceType.SolutionEntryIsLootEntry() || !sourceType.CanUpdateSourceLootEntry())
            return Queries.Empty(DataDatabaseType.World);

        if (difficultyId != 0)
            throw new Exception("Difficulty expected to be 0");
        
        if (rootLootEntries.Count > editorFeatures.GetMaxLootEntryForType(sourceType, difficultyId))
            throw new Exception($"Too many root loot entries for {sourceType} (max {editorFeatures.GetMaxLootEntryForType(sourceType, difficultyId)})");

        var lootId = rootLootEntries.Count > 0 ? (uint)rootLootEntries[0] : 0;

        switch (sourceType)
        {
            case LootSourceType.Creature:
                return Queries.Table(DatabaseTable.WorldTable("creature_template"))
                    .Where(row => row.Column<uint>("Entry") == solutionEntry)
                    .Set("LootId", lootId)
                    .Update();
            case LootSourceType.GameObject:
                return Queries.Table(DatabaseTable.WorldTable("gameobject_template"))
                    .Where(row => row.Column<uint>("Entry") == solutionEntry)
                    .Set("Data1", lootId)
                    .Update();
            case LootSourceType.Skinning:
                return Queries.Table(DatabaseTable.WorldTable("creature_template"))
                    .Where(row => row.Column<uint>("Entry") == solutionEntry)
                    .Set("SkinningLootId", lootId)
                    .Update();
            case LootSourceType.Pickpocketing:
                return Queries.Table(DatabaseTable.WorldTable("creature_template"))
                    .Where(row => row.Column<uint>("Entry") == solutionEntry)
                    .Set("PickpocketLootId", lootId)
                    .Update();
            default:
                throw new ArgumentOutOfRangeException(nameof(sourceType), sourceType, null);
        }
    }
}