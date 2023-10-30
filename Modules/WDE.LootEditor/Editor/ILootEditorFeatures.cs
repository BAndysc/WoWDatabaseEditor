using System.Collections.Generic;
using WDE.Common.Database;

namespace WDE.LootEditor.Editor;

public interface ILootEditorFeatures
{
    bool HasPatchField { get; }
    bool HasCommentField(LootSourceType lootType);
    bool HasLootModeField { get; }
    bool HasConditionId { get; }
    bool ItemCanBeCurrency { get; }
    bool LootGroupHasName(LootSourceType lootType);
    bool LootGroupHasFlags(LootSourceType lootType);
    int GetConditionSourceTypeFor(LootSourceType type);
    int GetMaxLootEntryForType(LootSourceType type, uint difficultyId);
    string GetTableNameFor(LootSourceType type);
    IReadOnlyList<LootSourceType> SupportedTypes { get; }
}