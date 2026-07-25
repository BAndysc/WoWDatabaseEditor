using System;
using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.AreaTriggers;

// CMaNGOS areatrigger side tables (the trigger shapes live in AreaTrigger.dbc). Same capability
// pattern as the safe-loc providers: no provider on other cores -> IQueryGenerator<T>.TableName
// is null and the 3D area trigger tool hides itself. areatrigger_teleport columns differ per
// expansion, hence one insert provider per core flavor emitting exactly its columns.

/// <summary>Which optional <c>areatrigger_teleport</c> columns the active core has - drives the
/// fields the 3D inspector shows and the columns the matching insert provider emits.</summary>
[Flags]
public enum AreaTriggerTeleportColumns
{
    None = 0,
    /// <summary>required_level, required_item, required_item2, required_quest_done</summary>
    Requirements = 1,
    /// <summary>heroic_key, heroic_key2, required_quest_done_heroic</summary>
    HeroicRequirements = 2,
    ConditionId = 4,
    Status = 8,
    StatusFailedText = 16,
}

[UniqueProvider]
public interface IAreaTriggerEditorConfig
{
    AreaTriggerTeleportColumns TeleportColumns { get; }
}

[FallbackAutoRegister]
internal class NullAreaTriggerEditorConfig : IAreaTriggerEditorConfig
{
    public AreaTriggerTeleportColumns TeleportColumns => AreaTriggerTeleportColumns.None;
}

[AutoRegister]
[RequiresCore("CMaNGOS-Classic")]
internal class MangosClassicAreaTriggerEditorConfig : IAreaTriggerEditorConfig
{
    public AreaTriggerTeleportColumns TeleportColumns =>
        AreaTriggerTeleportColumns.Requirements |
        AreaTriggerTeleportColumns.ConditionId |
        AreaTriggerTeleportColumns.StatusFailedText;
}

[AutoRegister]
[RequiresCore("CMaNGOS-TBC")]
internal class MangosTbcAreaTriggerEditorConfig : IAreaTriggerEditorConfig
{
    public AreaTriggerTeleportColumns TeleportColumns =>
        AreaTriggerTeleportColumns.Requirements |
        AreaTriggerTeleportColumns.HeroicRequirements |
        AreaTriggerTeleportColumns.ConditionId |
        AreaTriggerTeleportColumns.Status |
        AreaTriggerTeleportColumns.StatusFailedText;
}

[AutoRegister]
[RequiresCore("CMaNGOS-WoTLK")]
internal class MangosWrathAreaTriggerEditorConfig : IAreaTriggerEditorConfig
{
    public AreaTriggerTeleportColumns TeleportColumns =>
        AreaTriggerTeleportColumns.Requirements |
        AreaTriggerTeleportColumns.HeroicRequirements |
        AreaTriggerTeleportColumns.ConditionId;
}

[AutoRegister]
[RequiresCore("CMaNGOS-Classic")]
internal class MangosClassicAreaTriggerTeleportQueryProvider : BaseInsertQueryProvider<IAreaTriggerTeleport>,
    IDeleteQueryProvider<IAreaTriggerTeleport>
{
    protected override object Convert(IAreaTriggerTeleport t)
    {
        return new
        {
            id = t.Id,
            name = t.Name,
            required_level = t.RequiredLevel,
            required_item = t.RequiredItem,
            required_item2 = t.RequiredItem2,
            required_quest_done = t.RequiredQuestDone,
            target_map = t.Map,
            target_position_x = t.X,
            target_position_y = t.Y,
            target_position_z = t.Z,
            target_orientation = t.O,
            status_failed_text = t.StatusFailedText,
            condition_id = t.ConditionId,
        };
    }

    public IQuery Delete(IAreaTriggerTeleport t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("id") == t.Id)
            .Delete();
    }

    public override DatabaseTable TableName => DatabaseTable.WorldTable("areatrigger_teleport");
}

[AutoRegister]
[RequiresCore("CMaNGOS-TBC")]
internal class MangosTbcAreaTriggerTeleportQueryProvider : BaseInsertQueryProvider<IAreaTriggerTeleport>,
    IDeleteQueryProvider<IAreaTriggerTeleport>
{
    protected override object Convert(IAreaTriggerTeleport t)
    {
        return new
        {
            id = t.Id,
            name = t.Name,
            required_level = t.RequiredLevel,
            required_item = t.RequiredItem,
            required_item2 = t.RequiredItem2,
            heroic_key = t.HeroicKey,
            heroic_key2 = t.HeroicKey2,
            required_quest_done = t.RequiredQuestDone,
            required_quest_done_heroic = t.RequiredQuestDoneHeroic,
            target_map = t.Map,
            target_position_x = t.X,
            target_position_y = t.Y,
            target_position_z = t.Z,
            target_orientation = t.O,
            condition_id = t.ConditionId,
            status = t.Status,
            status_failed_text = t.StatusFailedText,
        };
    }

    public IQuery Delete(IAreaTriggerTeleport t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("id") == t.Id)
            .Delete();
    }

    public override DatabaseTable TableName => DatabaseTable.WorldTable("areatrigger_teleport");
}

[AutoRegister]
[RequiresCore("CMaNGOS-WoTLK")]
internal class MangosWrathAreaTriggerTeleportQueryProvider : BaseInsertQueryProvider<IAreaTriggerTeleport>,
    IDeleteQueryProvider<IAreaTriggerTeleport>
{
    protected override object Convert(IAreaTriggerTeleport t)
    {
        return new
        {
            id = t.Id,
            name = t.Name,
            required_level = t.RequiredLevel,
            required_item = t.RequiredItem,
            required_item2 = t.RequiredItem2,
            heroic_key = t.HeroicKey,
            heroic_key2 = t.HeroicKey2,
            required_quest_done = t.RequiredQuestDone,
            required_quest_done_heroic = t.RequiredQuestDoneHeroic,
            target_map = t.Map,
            target_position_x = t.X,
            target_position_y = t.Y,
            target_position_z = t.Z,
            target_orientation = t.O,
            condition_id = t.ConditionId,
        };
    }

    public IQuery Delete(IAreaTriggerTeleport t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("id") == t.Id)
            .Delete();
    }

    public override DatabaseTable TableName => DatabaseTable.WorldTable("areatrigger_teleport");
}

[AutoRegister]
[RequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
internal class MangosAreaTriggerTavernQueryProvider : BaseInsertQueryProvider<IAreaTriggerTavern>,
    IDeleteQueryProvider<IAreaTriggerTavern>
{
    protected override object Convert(IAreaTriggerTavern t)
    {
        return new
        {
            id = t.Id,
            name = t.Name,
        };
    }

    public IQuery Delete(IAreaTriggerTavern t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("id") == t.Id)
            .Delete();
    }

    public override DatabaseTable TableName => DatabaseTable.WorldTable("areatrigger_tavern");
}

[AutoRegister]
[RequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
internal class MangosAreaTriggerQuestRelationQueryProvider : BaseInsertQueryProvider<IAreaTriggerQuestRelation>,
    IDeleteQueryProvider<IAreaTriggerQuestRelation>
{
    protected override object Convert(IAreaTriggerQuestRelation t)
    {
        return new
        {
            id = t.Id,
            quest = t.Quest,
        };
    }

    public IQuery Delete(IAreaTriggerQuestRelation t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("id") == t.Id)
            .Delete();
    }

    public override DatabaseTable TableName => DatabaseTable.WorldTable("areatrigger_involvedrelation");
}

[AutoRegister]
[RequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
internal class MangosScriptedAreaTriggerQueryProvider : BaseInsertQueryProvider<IScriptedAreaTrigger>,
    IDeleteQueryProvider<IScriptedAreaTrigger>
{
    protected override object Convert(IScriptedAreaTrigger t)
    {
        return new
        {
            entry = t.Id,
            ScriptName = t.ScriptName,
        };
    }

    public IQuery Delete(IScriptedAreaTrigger t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("entry") == t.Id)
            .Delete();
    }

    public override DatabaseTable TableName => DatabaseTable.WorldTable("scripted_areatrigger");
}
