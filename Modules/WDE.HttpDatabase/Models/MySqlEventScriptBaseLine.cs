
using WDE.Common.Database;

namespace WDE.HttpDatabase.Models;


public class JsonWaypointScriptNoCommentLine : JsonEventScriptBaseLine
{
    public override EventScriptType Type => EventScriptType.Waypoint;
    
    public override string Comment => "";
}


public class JsonSpellScriptNoCommentLine : JsonEventScriptBaseLine
{
    public override EventScriptType Type => EventScriptType.Spell;
    
    
    public uint EffectIdx { get; set; }
    
    public override string Comment => "";
    
    public override uint? EffectIndex => EffectIdx;
}


public class JsonEventScriptNoCommentLine : JsonEventScriptBaseLine
{
    public override EventScriptType Type => EventScriptType.Event;

    public override string Comment => "";
}


public class JsonQuestStartScriptNoCommentLine : JsonEventScriptBaseLine
{
    public override EventScriptType Type => EventScriptType.QuestStart;

    public override string Comment => "";
}


public class JsonQuestEndScriptNoCommentLine : JsonEventScriptBaseLine
{
    public override EventScriptType Type => EventScriptType.QuestEnd;

    public override string Comment => "";
}


public class JsonGameObjectUseScriptNoCommentLine : JsonEventScriptBaseLine
{
    public override EventScriptType Type => EventScriptType.GameObjectUse;

    public override string Comment => "";
}


public class JsonWaypointScriptLine : JsonWaypointScriptNoCommentLine
{
    public override string Comment => comment;

     
    public string comment { get; set; } = "";
}


public class JsonSpellScriptLine : JsonSpellScriptNoCommentLine
{
    public override string Comment => comment;

     
    public string comment { get; set; } = "";
}



public class JsonEventScriptLine : JsonEventScriptNoCommentLine
{
    public override string Comment => comment;

     
    public string comment { get; set; } = "";
}

public abstract class JsonEventScriptBaseLine : IEventScriptLine
{
    public abstract EventScriptType Type { get; }
    public abstract string Comment { get; }

    
    public uint Id { get; set; }

    public virtual uint? EffectIndex => 0;

    
    public uint Delay { get; set; }

    
    public uint Command { get; set; }

    
    public uint DataLong1 { get; set; }

    
    public uint DataLong2 { get; set; }

    
    public int DataInt { get; set; }

    
    public float X { get; set; }

    
    public float Y { get; set; }

    
    public float Z { get; set; }

    
    public float O { get; set; }
}