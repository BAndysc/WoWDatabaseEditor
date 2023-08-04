using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.MySqlDatabaseCommon.CommonModels;

[Table(Name = "waypoint_scripts")]
public class MySqlWaypointScriptNoCommentLine : MySqlEventScriptBaseLine
{
    public override EventScriptType Type => EventScriptType.Waypoint;
    
    public override string Comment => "";
}

[Table(Name = "spell_scripts")]
public class MySqlSpellScriptNoCommentLine : MySqlEventScriptBaseLine
{
    public override EventScriptType Type => EventScriptType.Spell;
    
    [Column(Name = "effIndex")]
    public uint EffectIdx { get; set; }
    
    public override string Comment => "";
    
    public override uint? EffectIndex => EffectIdx;
}

[Table(Name = "event_scripts")]
public class MySqlEventScriptNoCommentLine : MySqlEventScriptBaseLine
{
    public override EventScriptType Type => EventScriptType.Event;

    public override string Comment => "";
}

[Table(Name = "quest_start_scripts")]
public class MySqlQuestStartScriptNoCommentLine : MySqlEventScriptBaseLine
{
    public override EventScriptType Type => EventScriptType.QuestStart;

    public override string Comment => "";
}

[Table(Name = "quest_end_scripts")]
public class MySqlQuestEndScriptNoCommentLine : MySqlEventScriptBaseLine
{
    public override EventScriptType Type => EventScriptType.QuestEnd;

    public override string Comment => "";
}

[Table(Name = "gameobject_scripts")]
public class MySqlGameObjectUseScriptNoCommentLine : MySqlEventScriptBaseLine
{
    public override EventScriptType Type => EventScriptType.GameObjectUse;

    public override string Comment => "";
}

[Table(Name = "waypoint_scripts")]
public class MySqlWaypointScriptLine : MySqlWaypointScriptNoCommentLine
{
    public override string Comment => comment;

    [Column(Name = "comment")] 
    public string comment { get; set; } = "";
}

[Table(Name = "spell_scripts")]
public class MySqlSpellScriptLine : MySqlSpellScriptNoCommentLine
{
    public override string Comment => comment;

    [Column(Name = "comment")] 
    public string comment { get; set; } = "";
}


[Table(Name = "event_scripts")]
public class MySqlEventScriptLine : MySqlEventScriptNoCommentLine
{
    public override string Comment => comment;

    [Column(Name = "comment")] 
    public string comment { get; set; } = "";
}

public abstract class MySqlEventScriptBaseLine : IEventScriptLine
{
    public abstract EventScriptType Type { get; }
    public abstract string Comment { get; }

    [Column(Name = "id")]
    public uint Id { get; set; }

    public virtual uint? EffectIndex => 0;

    [Column(Name = "delay")]
    public uint Delay { get; set; }

    [Column(Name = "command")]
    public uint Command { get; set; }

    [Column(Name = "datalong")]
    public uint DataLong1 { get; set; }

    [Column(Name = "datalong2")]
    public uint DataLong2 { get; set; }

    [Column(Name = "dataint")]
    public int DataInt { get; set; }

    [Column(Name = "x")]
    public float X { get; set; }

    [Column(Name = "y")]
    public float Y { get; set; }

    [Column(Name = "z")]
    public float Z { get; set; }

    [Column(Name = "o")]
    public float O { get; set; }
}