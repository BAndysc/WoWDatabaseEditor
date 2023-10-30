using System;
using System.Collections.Generic;
using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WDE.LootEditor.Editor;

[FallbackAutoRegister]
public class NullLootEditorFeatures : ILootEditorFeatures
{
    public bool HasPatchField => false;
    
    public bool HasLootModeField => false;
    
    public bool HasConditionId => false;
    
    public bool ItemCanBeCurrency => false;

    public bool HasCommentField(LootSourceType lootType) => false;

    public bool LootGroupHasName(LootSourceType lootType) => false;
    
    public bool LootGroupHasFlags(LootSourceType lootType) => false;
    
    public int GetConditionSourceTypeFor(LootSourceType type) => 0;

    public int GetMaxLootEntryForType(LootSourceType type, uint difficultyId) => 0;

    public string GetTableNameFor(LootSourceType type) => "";

    public IReadOnlyList<LootSourceType> SupportedTypes => Array.Empty<LootSourceType>();
}