using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using WDE.Conditions.Data;
using WDE.MVVM.Observable;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Editor.ViewModels;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Services;

public abstract class SmartHighlighter : ISmartHighlighter
{
    private readonly ISmartDataManager smartDataManager;
    private readonly IConditionDataManager conditionDataManager;

    private Dictionary<string, Dictionary<int, int>> highlightEventByParameter = new();
    private Dictionary<string, Dictionary<int, int>> highlightActionByParameter = new();
    private Dictionary<string, Dictionary<int, int>> highlightConditionsByParameter = new();

    private Dictionary<int, int>? eventsToHighlight = null;
    private Dictionary<int, int>? actionsToHighlight = null;
    private Dictionary<int, int>? conditionsToHighlight = null;

    public IReadOnlyList<HighlightViewModel> Highlighters => highlighters;
    protected List<HighlightViewModel> highlighters = new();
    
    public SmartHighlighter(ISmartDataManager smartDataManager, IConditionDataManager conditionDataManager)
    {
        this.smartDataManager = smartDataManager;
        this.conditionDataManager = conditionDataManager;

        smartDataManager.GetAllData(SmartType.SmartEvent)
            .CombineLatest(smartDataManager.GetAllData(SmartType.SmartAction))
            .SubscribeAction(tuple => Load(tuple.First, tuple.Second));
    }

    private void Load(IReadOnlyList<SmartGenericJsonData> events, IReadOnlyList<SmartGenericJsonData> actions)
    {
        highlightEventByParameter.Clear();
        highlightActionByParameter.Clear();
        highlightConditionsByParameter.Clear();
        highlighters.Clear();
        
        void SetupHighlight(string header, string parameter)
        {
            Dictionary<int, int> highlightEvents = new();
            Dictionary<int, int> highlightActions = new();
            Dictionary<int, int> highlightConditions = new();
            
            foreach (var data in events)
            {
                if (data.Parameters == null)
                    continue;
    
                int index = 0;
                foreach (var p in data.Parameters)
                {
                    if (p.Type == parameter)
                        highlightEvents.Add(data.Id, index);
                    index++;
                }
            }
            
            foreach (var data in actions)
            {
                if (data.Parameters == null)
                    continue;
    
                int index = 0;
                foreach (var p in data.Parameters)
                {
                    if (p.Type == parameter)
                    {
                        // in fact, more than one parameter can have the same type
                        // but let's ignore it now and just pick the first matching one
                        highlightActions.Add(data.Id, index);
                        break;
                    }
                    index++;
                }
            }
    
            foreach (var data in conditionDataManager.AllConditionData)
            {
                if (data.Parameters == null)
                    continue;
                
                int index = 0;
                foreach (var p in data.Parameters)
                {
                    if (p.Type == parameter)
                        highlightConditions.Add(data.Id, index);
                    index++;
                }
            }
    
            highlightEventByParameter[parameter] = highlightEvents;
            highlightActionByParameter[parameter] = highlightActions;
            highlightConditionsByParameter[parameter] = highlightConditions;
            highlighters.Add(new(header, parameter, highlighters.Count));
        }
        
        highlighters.Add(new("None", "", 0));
        SetupHighlight("Timed events", "TimedEventParameter");
        SetupHighlight("Links", "LinkParameter");
        SetupHighlight("Spells", "SpellParameter");
        DoSetup(SetupHighlight);
    }

    protected virtual void DoSetup(Action<string, string> setupHighlight)
    {
        
    }

    public void SetHighlightParameter(string? parameter)
    {
        if (string.IsNullOrEmpty(parameter))
        {
            actionsToHighlight = null;
            eventsToHighlight = null;
            conditionsToHighlight = null;
            return;
        }
        
        if (highlightActionByParameter.TryGetValue(parameter, out var actions))
            actionsToHighlight = actions;
        else
            actionsToHighlight = null;

        if (highlightEventByParameter.TryGetValue(parameter, out var events))
            eventsToHighlight = events;
        else
            eventsToHighlight = null;

        if (highlightConditionsByParameter.TryGetValue(parameter, out var conditions))
            conditionsToHighlight = conditions;
        else
            conditionsToHighlight = null;
    }

    public bool Highlight(SmartEvent e, out int parameterIndex)
    {
        parameterIndex = 0;
        return eventsToHighlight?.TryGetValue(e.Id, out parameterIndex) ?? false;
    }

    public bool Highlight(SmartAction a, out int parameterIndex)
    {
        parameterIndex = 0;
        return actionsToHighlight?.TryGetValue(a.Id, out parameterIndex) ?? false;
    }
    
    public bool Highlight(SmartCondition c, out int parameterIndex)
    {
        parameterIndex = 0;
        return conditionsToHighlight?.TryGetValue(c.Id, out parameterIndex) ?? false;
    }
}