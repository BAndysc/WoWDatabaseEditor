using SmartFormat;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.EventScriptsEditor.EventScriptData;
using WDE.EventScriptsEditor.ViewModels;
using WDE.Module.Attributes;

namespace WDE.EventScriptsEditor.Services;

[AutoRegister]
[SingleInstance]
public class EventScriptViewModelFactory : IEventScriptViewModelFactory
{
    private readonly IEventScriptDataProvider dataProvider;
    private readonly IParameterFactory parameterFactory;

    public EventScriptViewModelFactory(IEventScriptDataProvider dataProvider,
        IParameterFactory parameterFactory)
    {
        this.dataProvider = dataProvider;
        this.parameterFactory = parameterFactory;
    }
    
    public EventScriptLineViewModel Factory(IEventScriptLine line)
    {
        string source, target, sourceOrTargetCreature, sourceOrTargetPlayer;
        switch (line.Type)
        {
            case EventScriptType.Event:
                source = "Triggering player";
                target = "Triggered game object";
                sourceOrTargetPlayer = source;
                sourceOrTargetCreature = "(null)";
                break;
            case EventScriptType.Spell:
                source = "Caster";
                target = "Hit target";
                sourceOrTargetPlayer = "Caster player or hit target player";
                sourceOrTargetCreature = "Caster creature or hit target creature";
                break;
            case EventScriptType.Waypoint:
                source = "Creature";
                target = "(null)";
                sourceOrTargetPlayer = target;
                sourceOrTargetCreature = source;
                break;
            case EventScriptType.Gossip:
                source = "Creature";
                target = "(null)";
                sourceOrTargetPlayer = target;
                sourceOrTargetCreature = source;
                break;
            case EventScriptType.QuestStart:
                source = "Quest giver";
                target = "Player";
                sourceOrTargetPlayer = target;
                sourceOrTargetCreature = source;
                break;
            case EventScriptType.QuestEnd:
                source = "Quest ender";
                target = "Player";
                sourceOrTargetPlayer = target;
                sourceOrTargetCreature = source;
                break;
            case EventScriptType.GameObjectUse:
                source = "Player";
                target = "Gameobject";
                sourceOrTargetPlayer = target;
                sourceOrTargetCreature = source;
                break;
            default:
                source = target = sourceOrTargetPlayer = sourceOrTargetCreature = "(non supported script type)";
                break;
        }
        
        var data = dataProvider.GetEventScriptData(line.Command);
        if (data == null)
        {
            var invalid = $"Invalid line: {line.Command}, {line.DataLong1}, {line.DataLong2}, {line.DataInt}";
            return new EventScriptLineViewModel(invalid, line.Comment);
        }
        else
        {
            var dataLongParam = parameterFactory.Factory(data.DataLong);
            var dataLong2Param = parameterFactory.Factory(data.DataLong2);
            var dataIntParam = parameterFactory.Factory(data.DataInt);

            var dataLong = dataLongParam.ToString((long)line.DataLong1);
            var dataLong2 = dataLong2Param.ToString((long)line.DataLong2);
            var dataInt = dataIntParam.ToString(line.DataInt);
                
            string text = Smart.Format(data.Description,
                new
                {
                    datalongvalue = line.DataLong1,
                    datalong2value = line.DataLong1,
                    dataintvalue = line.DataInt,
                    datalong = dataLong,
                    datalong2 = dataLong2,
                    dataint = dataInt,
                    source = source,
                    target = target,
                    sourceOrTargetCreature = sourceOrTargetCreature,
                    sourceOrTargetPlayer = sourceOrTargetPlayer,
                    x = line.X,
                    y = line.Y,
                    z = line.Z,
                    o = line.O
                });
            return new EventScriptLineViewModel(text, line.Comment);
        }
    }
}