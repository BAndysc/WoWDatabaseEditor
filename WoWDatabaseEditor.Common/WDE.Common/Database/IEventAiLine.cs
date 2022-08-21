using System;

namespace WDE.Common.Database;

public interface IEventAiLine
{
    public uint Id { get; }
    public int CreatureIdOrGuid { get; }
    public byte EventType { get; }
    public int EventInversePhaseMask { get; }
    public uint EventChance { get; }
    public uint EventFlags { get; }
    public int EventParam1 { get; }
    public int EventParam2 { get; }
    public int EventParam3 { get; }
    public int EventParam4 { get; }
    public int EventParam5 { get; }
    public int EventParam6 { get; }
    public uint Action1Type { get; }
    public int Action1Param1 { get; }
    public int Action1Param2 { get; }
    public int Action1Param3 { get; }
    public uint Action2Type { get; }
    public int Action2Param1 { get; }
    public int Action2Param2 { get; }
    public int Action2Param3 { get; }
    public uint Action3Type { get; }
    public int Action3Param1 { get; }
    public int Action3Param2 { get; }
    public int Action3Param3 { get; }
    public string Comment { get; }
}

public static class EventAiExtensions
{
    public static int GetEventParam(this IEventAiLine line, int i)
    {
        switch (i)
        {
            case 0: return line.EventParam1;
            case 1: return line.EventParam2;
            case 2: return line.EventParam3;
            case 3: return line.EventParam4;
            case 4: return line.EventParam5;
            case 5: return line.EventParam6;
        }
        throw new ArgumentOutOfRangeException(nameof(i));
    }

    public static uint GetActionType(this IEventAiLine line,int i)
    {
        switch (i)
        {
            case 0: return line.Action1Type;
            case 1: return line.Action2Type;
            case 2: return line.Action3Type;
        }
        throw new ArgumentOutOfRangeException(nameof(i));
    }

    public static int GetActionParameter(this IEventAiLine line, int action, int index)
    {
        switch (action, index)
        {
            case (0, 0): return line.Action1Param1;
            case (0, 1): return line.Action1Param2;
            case (0, 2): return line.Action1Param3;
            case (1, 0): return line.Action2Param1;
            case (1, 1): return line.Action2Param2;
            case (1, 2): return line.Action2Param3;
            case (2, 0): return line.Action3Param1;
            case (2, 1): return line.Action3Param2;
            case (2, 2): return line.Action3Param3;
        }
        throw new ArgumentOutOfRangeException();
    }
}

public class AbstractEventAiLine : IEventAiLine
{
    public uint Id { get; set; }
    public int CreatureIdOrGuid  { get; set; }
    public byte EventType { get; set; }
    public int EventInversePhaseMask { get; set; }
    public uint EventChance { get; set; }
    public uint EventFlags { get; set; }
    public int EventParam1 { get; set; }
    public int EventParam2 { get; set; }
    public int EventParam3 { get; set; }
    public int EventParam4 { get; set; }
    public int EventParam5 { get; set; }
    public int EventParam6 { get; set; }
    public uint Action1Type { get; set; }
    public int Action1Param1 { get; set; }
    public int Action1Param2 { get; set; }
    public int Action1Param3 { get; set; }
    public uint Action2Type { get; set; }
    public int Action2Param1 { get; set; }
    public int Action2Param2 { get; set; }
    public int Action2Param3 { get; set; }
    public uint Action3Type { get; set; }
    public int Action3Param1 { get; set; }
    public int Action3Param2 { get; set; }
    public int Action3Param3 { get; set; }
    public string Comment { get; set; } = "";

    public void SetEventParam(int index, int value)
    {
        switch (index)
        {
            case 0:
                EventParam1 = value;
                break;
            case 1:
                EventParam2 = value;
                break;
            case 2:
                EventParam3 = value;
                break;
            case 3:
                EventParam4 = value;
                break;
            case 4:
                EventParam5 = value;
                break;
            case 5:
                EventParam6 = value;
                break;
        }
    }

    public void SetActionType(int actionId, uint type)
    {
        switch (actionId)
        {
            case 0:
                Action1Type = type;
                break;
            case 1:
                Action2Type = type;
                break;
            case 2:
                Action3Type = type;
                break;
        }
    }

    public void SetActionParam(int actionId, int paramIndex, int value)
    {
        switch (actionId, paramIndex)
        {
            case (0, 0):
                Action1Param1 = value;
                break;
            case (0, 1):
                Action1Param2 = value;
                break;
            case (0, 2):
                Action1Param3 = value;
                break;
            
            case (1, 0):
                Action2Param1 = value;
                break;
            case (1, 1):
                Action2Param2 = value;
                break;
            case (1, 2):
                Action2Param3 = value;
                break;
            
            case (2, 0):
                Action3Param1 = value;
                break;
            case (2, 1):
                Action3Param2 = value;
                break;
            case (2, 2):
                Action3Param3 = value;
                break;
        }
    }
}