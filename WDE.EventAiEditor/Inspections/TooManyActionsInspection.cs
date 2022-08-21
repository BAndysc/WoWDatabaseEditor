using System;
using System.Collections.Generic;
using WDE.Common.Managers;
using WDE.EventAiEditor.Data;
using WDE.EventAiEditor.Models;

namespace WDE.EventAiEditor.Inspections;

public class TooManyActionsInspection : IEventInspection
{
    private readonly Dictionary<uint, (int min, int max)> eventIdToMinMaxIndex = new();
    
    public TooManyActionsInspection(IEventAiDataManager dataManager)
    {
        foreach (var data in dataManager.GetAllData(EventOrAction.Event))
        {
            if (data.Parameters == null)
                continue;

            int? minIndex = null;
            int? maxIndex = null;
            for (var index = 0; index < data.Parameters.Count; index++)
            {
                var param = data.Parameters[index];
                if (param.Name.Contains("Repeat Min", StringComparison.InvariantCultureIgnoreCase) ||
                    param.Name.Contains("Cooldown Min", StringComparison.InvariantCultureIgnoreCase))
                    minIndex = index;
                else if (param.Name.Contains("Repeat Max", StringComparison.InvariantCultureIgnoreCase) ||
                         param.Name.Contains("Cooldown Max", StringComparison.InvariantCultureIgnoreCase))
                    maxIndex = index;
            }

            if (minIndex.HasValue && maxIndex.HasValue)
                eventIdToMinMaxIndex[data.Id] = (minIndex.Value, maxIndex.Value);
        }
    }

    private enum Reason
    {
        Ok,
        Chance,
        CombatFlag,
        RandomFlag,
        RepeatTimers
    }
    
    private Reason AllowMultipleActions(EventAiEvent e)
    {
        if (e.Chance.Value < 100)
            return Reason.Chance;

        var flags = (EventAiFlag)e.Flags.Value;
        
        if (flags.HasFlagFast(EventAiFlag.CombatAction))
            return Reason.CombatFlag;

        if (flags.HasFlagFast(EventAiFlag.RandomAction))
            return Reason.RandomFlag;

        if (eventIdToMinMaxIndex.TryGetValue(e.Id, out var minMaxCooldown))
        {
            var min = e.GetParameter(minMaxCooldown.min).Value;
            var max = e.GetParameter(minMaxCooldown.max).Value;
            if (min != max)
                return Reason.RepeatTimers;
        }
        
        return Reason.Ok;
    }
    
    public InspectionResult? Inspect(EventAiEvent e)
    {
        if (e.Actions.Count > 3)
        {
            var problem = AllowMultipleActions(e);
            if (problem == Reason.Ok)
                return null;

            string msg = null!;
            switch (problem)
            {
                case Reason.Chance:
                    msg = "the event has non 100% chance, so automatic splitting would case that different actions would be separately randomly executed";
                    break;
                case Reason.CombatFlag:
                    msg = "the event has the COMBAT flag. With this flag, when one action fails, other will not be executed. Thus it is not possible to split automatically the event into two";
                    break;
                case Reason.RandomFlag:
                    msg = "the event has the RANDOM flag, but it is not possible to have more than 3 random flags";
                    break;
                case Reason.RepeatTimers:
                    msg = "the event has random timers, thus it is not possible to split automatically the event into two, because those two events will have their own random timers";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            return new InspectionResult()
            {
                Line = e.LineId,
                Severity = DiagnosticSeverity.Critical,
                Message = $"You can't have more than 3 actions in this event, you have to split the event. Reason: {msg}."
            };
        }

        return null;
    }
}