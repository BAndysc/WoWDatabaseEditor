using System;
using System.Collections.Generic;
using WDE.Common.Utils;

namespace WDE.Common.Database
{
    public interface ISmartScriptLine
    {
        uint? CreatureEntry { get; }
        
        int EntryOrGuid { get; }

        int ScriptSourceType { get; }

        int Id { get; }

        int Link { get; }
        
        int LineId { get; }

        SmallReadOnlyList<uint>? Difficulties { get; }
        
        int EventType { get; }

        int EventPhaseMask { get; }

        int EventChance { get; }

        int TimerId { get; }
        
        int EventFlags { get; }

        long EventParam1 { get; }

        long EventParam2 { get; }

        long EventParam3 { get; }

        long EventParam4 { get; }

        long EventParam5 { get; }

        long EventParam6 { get; }

        long EventParam7 { get; }

        long EventParam8 { get; }
        
        float EventFloatParam1 { get; }
        
        float EventFloatParam2 { get; }

        string? EventStringParam { get; }

        int EventCooldownMin { get; }

        int EventCooldownMax { get; }

        int ActionType { get; set; }

        long ActionParam1 { get; }

        long ActionParam2 { get; }

        long ActionParam3 { get; }

        long ActionParam4 { get; }

        long ActionParam5 { get; }

        long ActionParam6 { get; }
        
        long ActionParam7 { get; }
        
        long ActionParam8 { get; }
        
        float ActionFloatParam1 { get; }
        
        float ActionFloatParam2 { get; }

        int SourceType { get; }

        long SourceParam1 { get; }

        long SourceParam2 { get; }

        long SourceParam3 { get; }

        int SourceConditionId { get; }

        float SourceX { get; }

        float SourceY { get; }

        float SourceZ { get; }

        float SourceO { get; }

        int TargetType { get; }

        long TargetParam1 { get; }

        long TargetParam2 { get; }

        long TargetParam3 { get; }

        int TargetConditionId { get; }

        float TargetX { get; }

        float TargetY { get; }

        float TargetZ { get; }

        float TargetO { get; }

        string Comment { get; set; }
    }

    public class AbstractSmartScriptLine : ISmartScriptLine
    {
        public uint? CreatureEntry { get; set; }
        public int EntryOrGuid { get; set; }
        public int ScriptSourceType { get; set; }
        public int Id { get; set; }
        public int Link { get; set; }
        public int LineId { get; set; }
        public SmallReadOnlyList<uint>? Difficulties { get; set; }
        public int EventType { get; set; }
        public int EventPhaseMask { get; set; }
        public int EventChance { get; set; }
        public int TimerId { get; set; }
        public int EventFlags { get; set; }
        public long EventParam1 { get; set; }
        public long EventParam2 { get; set; }
        public long EventParam3 { get; set; }
        public long EventParam4 { get; set; }
        public long EventParam5 { get; set; }
        public long EventParam6 { get; set; }
        public long EventParam7 { get; set; }
        public long EventParam8 { get; set; }
        public float EventFloatParam1 { get; set; }
        public float EventFloatParam2 { get; set; }
        public string? EventStringParam { get; set; }
        public int EventCooldownMin { get; set; }
        public int EventCooldownMax { get; set; }
        public int ActionType { get; set; }
        public long ActionParam1 { get; set; }
        public long ActionParam2 { get; set; }
        public long ActionParam3 { get; set; }
        public long ActionParam4 { get; set; }
        public long ActionParam5 { get; set; }
        public long ActionParam6 { get; set; }
        public long ActionParam7 { get; set; }
        public long ActionParam8 { get; set; }
        public float ActionFloatParam1 { get; set; }
        public float ActionFloatParam2 { get; set; }
        public int SourceType { get; set; }
        public long SourceParam1 { get; set; }
        public long SourceParam2 { get; set; }
        public long SourceParam3 { get; set; }
        public int SourceConditionId { get; set; }
        public float SourceX { get; set; }
        public float SourceY { get; set; }
        public float SourceZ { get; set; }
        public float SourceO { get; set; }
        public int TargetType { get; set; }
        public long TargetParam1 { get; set; }
        public long TargetParam2 { get; set; }
        public long TargetParam3 { get; set; }
        public int TargetConditionId { get; set; }
        public float TargetX { get; set; }
        public float TargetY { get; set; }
        public float TargetZ { get; set; }
        public float TargetO { get; set; }
        public string Comment { get; set; } = "";
    }

    public static class SmartScriptLineExtensions
    {
        public static long GetEventParam(this ISmartScriptLine line, int i)
        {
            switch (i)
            {
                case 0:
                    return line.EventParam1;
                case 1:
                    return line.EventParam2;
                case 2:
                    return line.EventParam3;
                case 3:
                    return line.EventParam4;
                case 4:
                    return line.EventParam5;
                case 5:
                    return line.EventParam6;
                case 6:
                    return line.EventParam7;
                case 7:
                    return line.EventParam8;
            }

            throw new IndexOutOfRangeException();
        }
        
        public static float GetEventFloatParam(this ISmartScriptLine line, int i)
        {
            switch (i)
            {
                case 0:
                    return line.EventFloatParam1;
                case 1:
                    return line.EventFloatParam2;
            }

            throw new IndexOutOfRangeException();
        }
        
        public static string? GetEventStringParam(this ISmartScriptLine line, int i)
        {
            switch (i)
            {
                case 0:
                    return line.EventStringParam;
            }

            throw new IndexOutOfRangeException();
        }
        
        public static long GetActionParam(this ISmartScriptLine line, int i)
        {
            switch (i)
            {
                case 0:
                    return line.ActionParam1;
                case 1:
                    return line.ActionParam2;
                case 2:
                    return line.ActionParam3;
                case 3:
                    return line.ActionParam4;
                case 4:
                    return line.ActionParam5;
                case 5:
                    return line.ActionParam6;
                case 6:
                    return line.ActionParam7;
                case 7:
                    return line.ActionParam8;
            }

            throw new IndexOutOfRangeException();
        }
        
        public static float GetActionFloatParam(this ISmartScriptLine line, int i)
        {
            switch (i)
            {
                case 0:
                    return line.ActionFloatParam1;
                case 1:
                    return line.ActionFloatParam2;
            }

            throw new IndexOutOfRangeException();
        }

        public static long GetSourceParam(this ISmartScriptLine line, int i)
        {
            switch (i)
            {
                case 0:
                    return line.SourceParam1;
                case 1:
                    return line.SourceParam2;
                case 2:
                    return line.SourceParam3;
            }

            throw new IndexOutOfRangeException();
        }
        
        public static long GetTargetParam(this ISmartScriptLine line, int i)
        {
            switch (i)
            {
                case 0:
                    return line.TargetParam1;
                case 1:
                    return line.TargetParam2;
                case 2:
                    return line.TargetParam3;
            }

            throw new IndexOutOfRangeException();
        }
    }
}