﻿using System;

namespace WDE.Common.Database
{
    public interface ISmartScriptLine
    {
        int EntryOrGuid { get; set; }

        int ScriptSourceType { get; set; }

        int Id { get; set; }

        int Link { get; set; }

        int EventType { get; set; }

        int EventPhaseMask { get; set; }

        int EventChance { get; set; }

        int EventFlags { get; set; }

        int EventParam1 { get; set; }

        int EventParam2 { get; set; }

        int EventParam3 { get; set; }

        int EventParam4 { get; set; }

        int EventCooldownMin { get; set; }

        int EventCooldownMax { get; set; }

        int ActionType { get; set; }

        int ActionParam1 { get; set; }

        int ActionParam2 { get; set; }

        int ActionParam3 { get; set; }

        int ActionParam4 { get; set; }

        int ActionParam5 { get; set; }

        int ActionParam6 { get; set; }

        int SourceType { get; set; }

        int SourceParam1 { get; set; }

        int SourceParam2 { get; set; }

        int SourceParam3 { get; set; }

        int SourceConditionId { get; set; }

        int TargetType { get; set; }

        int TargetParam1 { get; set; }

        int TargetParam2 { get; set; }

        int TargetParam3 { get; set; }

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
        public int EventType { get; set; }
        public int EventPhaseMask { get; set; }
        public int EventChance { get; set; }
        public int EventFlags { get; set; }
        public int EventParam1 { get; set; }
        public int EventParam2 { get; set; }
        public int EventParam3 { get; set; }
        public int EventParam4 { get; set; }
        public int EventCooldownMin { get; set; }
        public int EventCooldownMax { get; set; }
        public int ActionType { get; set; }
        public int ActionParam1 { get; set; }
        public int ActionParam2 { get; set; }
        public int ActionParam3 { get; set; }
        public int ActionParam4 { get; set; }
        public int ActionParam5 { get; set; }
        public int ActionParam6 { get; set; }
        public int SourceType { get; set; }
        public int SourceParam1 { get; set; }
        public int SourceParam2 { get; set; }
        public int SourceParam3 { get; set; }
        public int SourceConditionId { get; set; }
        public int TargetType { get; set; }
        public int TargetParam1 { get; set; }
        public int TargetParam2 { get; set; }
        public int TargetParam3 { get; set; }
        public int TargetConditionId { get; set; }
        public float TargetX { get; set; }
        public float TargetY { get; set; }
        public float TargetZ { get; set; }
        public float TargetO { get; set; }
        public string Comment { get; set; }
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