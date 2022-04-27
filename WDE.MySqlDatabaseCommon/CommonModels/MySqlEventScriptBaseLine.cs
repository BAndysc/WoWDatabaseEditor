using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.MySqlDatabaseCommon.CommonModels;

[Table(Name = "waypoint_scripts")]
public class MySqlWaypointScriptLine : MySqlEventScriptBaseLine
{
    public override EventScriptType Type => EventScriptType.Waypoint;
}

[Table(Name = "spell_scripts")]
public class MySqlSpellScriptLine : MySqlEventScriptBaseLine
{
    public override EventScriptType Type => EventScriptType.Spell;
    
    [Column(Name = "effIndex")]
    public uint EffectIdx { get; set; }

    public override uint? EffectIndex => EffectIdx;
}

[Table(Name = "event_scripts")]
public class MySqlEventScriptLine : MySqlEventScriptBaseLine
{
    public override EventScriptType Type => EventScriptType.Event;
}

public abstract class MySqlEventScriptBaseLine : IEventScriptLine
{
    public abstract EventScriptType Type { get; }

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

    [Column(Name = "comment")] 
    public string Comment { get; set; } = "";
}