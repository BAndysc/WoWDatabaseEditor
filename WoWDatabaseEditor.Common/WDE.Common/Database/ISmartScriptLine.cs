using System;

namespace WDE.Common.Database
{
    public interface ISmartScriptLine
    {
        int EntryOrGuid { get; set; }

        int ScriptSourceType { get; set; }

        int Id { get; set; }

        int Link { get; set; }
        
        int LineId { get; set; }

        int EventType { get; set; }

        int EventPhaseMask { get; set; }

        int EventChance { get; set; }

        int TimerId { get; set; }
        
        int EventFlags { get; set; }

        long EventParam1 { get; set; }

        long EventParam2 { get; set; }

        long EventParam3 { get; set; }

        long EventParam4 { get; set; }

        long EventParam5 { get; set; }
        
        float EventFloatParam1 { get; set; }
        
        float EventFloatParam2 { get; set; }

        string? EventStringParam { get; set; }

        int EventCooldownMin { get; set; }

        int EventCooldownMax { get; set; }

        int ActionType { get; set; }

        long ActionParam1 { get; set; }

        long ActionParam2 { get; set; }

        long ActionParam3 { get; set; }

        long ActionParam4 { get; set; }

        long ActionParam5 { get; set; }

        long ActionParam6 { get; set; }
        
        long ActionParam7 { get; set; }
        
        float ActionFloatParam1 { get; set; }
        
        float ActionFloatParam2 { get; set; }

        int SourceType { get; set; }

        long SourceParam1 { get; set; }

        long SourceParam2 { get; set; }

        long SourceParam3 { get; set; }

        int SourceConditionId { get; set; }

        float SourceX { get; set; }

        float SourceY { get; set; }

        float SourceZ { get; set; }

        float SourceO { get; set; }

        int TargetType { get; set; }

        long TargetParam1 { get; set; }

        long TargetParam2 { get; set; }

        long TargetParam3 { get; set; }

        int TargetConditionId { get; set; }

        float TargetX { get; set; }

        float TargetY { get; set; }

        float TargetZ { get; set; }

        float TargetO { get; set; }

        string Comment { get; set; }
    }

    public class AbstractSmartScriptLine : ISmartScriptLine
    {
        public int EntryOrGuid { get; set; }
        public int ScriptSourceType { get; set; }
        public int Id { get; set; }
        public int Link { get; set; }
        public int LineId { get; set; }
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