using System;
using Microsoft.Extensions.Logging;
using PropertyChanged.SourceGenerator;
using WDE.Common.Database;

namespace WDE.DatabaseDefinitionEditor.ViewModels.DefinitionEditor.MetaColumns;

public partial class LootMetaColumnViewModel : BaseMetaColumnViewModel
{
    [Notify] private LootSourceType lootType;
    [Notify] private string lootIdColumn;
    [Notify] private bool rawId;

    public override string Export()
    {
        return "loot:" + lootType.ToString() + ";" + lootIdColumn + ";" + (rawId ? "raw" : "");
    }

    public LootMetaColumnViewModel(ColumnViewModel parent) : base(parent)
    {
        lootIdColumn = "";
        rawId = true;
    }

    public LootMetaColumnViewModel(ColumnViewModel parent, string description) : base(parent)
    {
        if (!description.StartsWith("loot:"))
            throw new ArgumentException("Expected loot:");
        var parts = description.Substring("loot:".Length).Split(';');
        if (parts.Length != 2 && parts.Length != 3)
            throw new ArgumentException("Can't parse description: " + description + " (expected loot:[type];column format;[raw|])");
        if (Enum.TryParse<LootSourceType>(parts[0], out var _lootType))
            lootType = _lootType;
        else
            LOG.LogError("Can't parse loot type" + parts[0]);
        lootIdColumn = parts[1];
        rawId = parts.Length >= 3 && parts[2] == "raw";
    }
}