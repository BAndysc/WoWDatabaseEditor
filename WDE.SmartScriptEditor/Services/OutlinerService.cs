using System;
using System.Collections;
using System.Collections.Generic;
using Prism.Events;
using WDE.Common.Database;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Outliner;
using WDE.Common.Services.FindAnywhere;
using WDE.Common.Solution;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Editor;
using WDE.SmartScriptEditor.Editor.ViewModels;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Services;

public interface IOutlinerService
{
    void Process(ISmartScriptOutlinerModel outlinerModel, SmartScript script);
}

public interface ISmartScriptOutlinerModel : IOutlinerModel
{
    void Clear();
    void Add(SmartScriptType type, int entry);
}

[AutoRegister]
public class OutlinerModel : ISmartScriptOutlinerModel
{
    private readonly ISolutionItemIconRegistry iconRegistry;
    private readonly ISolutionItemNameRegistry nameRegistry;
    private readonly IEditorFeatures editorFeatures;
    private readonly ISmartScriptFactory factory;

    public OutlinerModel(IEditorFeatures editorFeatures,
        ISmartScriptFactory factory,
        ISolutionItemIconRegistry iconRegistry,
        ISolutionItemNameRegistry nameRegistry)
    {
        this.editorFeatures = editorFeatures;
        this.factory = factory;
        this.iconRegistry = iconRegistry;
        this.nameRegistry = nameRegistry;
    }

    private HashSet<(SmartScriptType type, int entry)> data = new();
    
    public void Clear() => data.Clear();

    public void Add(SmartScriptType type, int entry) => data.Add((type, entry));

    public IEnumerator<(RelatedSolutionItem.RelatedType, IFindAnywhereResult)> GetEnumerator()
    {
        foreach (var (type, entry) in data)
        {
            var solutionItem = editorFeatures.HasCreatureEntry ? factory.Factory((uint)entry, 0, type) : factory.Factory(null, entry, type);
            var relatedType = GetRelatedTypeForScriptType(type);
            yield return (relatedType, new FindAnywhereResult(
                iconRegistry.GetIcon(solutionItem),
                entry,
                nameRegistry.GetName(solutionItem),
                "",
                solutionItem
            ));
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    
    
    private static RelatedSolutionItem.RelatedType GetRelatedTypeForScriptType(SmartScriptType type)
    {
        switch (type)
        {
            case SmartScriptType.Creature:
                return RelatedSolutionItem.RelatedType.CreatureEntry;
            case SmartScriptType.GameObject:
                return RelatedSolutionItem.RelatedType.GameobjectEntry;
            case SmartScriptType.Template:
                return RelatedSolutionItem.RelatedType.Template;
            case SmartScriptType.TimedActionList:
                return RelatedSolutionItem.RelatedType.TimedActionList;
            case SmartScriptType.Quest:
                return RelatedSolutionItem.RelatedType.QuestEntry;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), $"{type} is not expected here, because there is no matching related Type");
        }
    }
}

[AutoRegister]
[SingleInstance]
public class OutlinerService : IOutlinerService
{
    private readonly ISmartScriptFactory smartScriptFactory;
    private readonly ISolutionItemIconRegistry iconRegistry;
    private readonly ISolutionItemNameRegistry nameRegistry;
    private readonly IEventAggregator eventAggregator;
    private SmartScriptType?[,] eventInspectors;
    private SmartScriptType?[,] actionInspectors;
    private SmartScriptType?[,] targetInspectors;
    
    public OutlinerService(ISmartDataManager dataManager, 
        IEditorFeatures editorFeatures, 
        ISmartScriptFactory smartScriptFactory,
        ISolutionItemIconRegistry iconRegistry,
        ISolutionItemNameRegistry nameRegistry,
        IEventAggregator eventAggregator)
    {
        this.smartScriptFactory = smartScriptFactory;
        this.iconRegistry = iconRegistry;
        this.nameRegistry = nameRegistry;
        this.eventAggregator = eventAggregator;
        eventInspectors = new SmartScriptType?[0, editorFeatures.EventParametersCount.IntCount];
        actionInspectors = new SmartScriptType?[0, editorFeatures.ActionParametersCount.IntCount];
        targetInspectors = new SmartScriptType?[0, editorFeatures.TargetParametersCount.IntCount];

        dataManager.GetAllData(SmartType.SmartEvent)
            .SubscribeAction(x => ProcessData(x, dataManager.MaxId(SmartType.SmartEvent)+1, ref eventInspectors));
        
        dataManager.GetAllData(SmartType.SmartAction)
            .SubscribeAction(x => ProcessData(x, dataManager.MaxId(SmartType.SmartAction)+1, ref actionInspectors));
        
        dataManager.GetAllData(SmartType.SmartTarget)
            .SubscribeAction(x => ProcessData(x, dataManager.MaxId(SmartType.SmartTarget)+1, ref targetInspectors));
        
        void ProcessData(IReadOnlyList<SmartGenericJsonData> list, int maxId, ref SmartScriptType?[,] dest)
        {
            if (dest.GetLength(0) < maxId)
                dest = new SmartScriptType?[maxId, dest.GetLength(1)];
            else
                Array.Clear(dest);
            foreach (var data in list)
            {
                if (data.Parameters == null)
                    continue;

                for (int i = 0; i < data.Parameters.Count; ++i)
                {
                    dest[data.Id, i] = GetInspectorForParameterType(data.Parameters[i].Type);
                }
            }
        }
    }

    private SmartScriptType? GetInspectorForParameterType(string parameter)
    {
        if (parameter == "CreatureParameter")
            return SmartScriptType.Creature;
        if (parameter == "GameobjectParameter")
            return SmartScriptType.GameObject;
        if (parameter == "TimedActionListParameter")
            return SmartScriptType.TimedActionList;
        if (parameter == "QuestParameter")
            return SmartScriptType.Quest;
        if (parameter == "AITemplateParameter")
            return SmartScriptType.Template;

        return null;
    }
    
    public void Process(ISmartScriptOutlinerModel outlinerModel, SmartScript script)
    {
        outlinerModel.Clear();
        foreach (var e in script.Events)
        {
            ProcessGenericElement(outlinerModel, e, eventInspectors);
            foreach (var action in e.Actions)
            {
                if (action.Id == SmartConstants.ActionCallRandomRangeTimedActionList)
                    ProcessRandomRangeTimedActionList(outlinerModel, action);
                else
                    ProcessGenericElement(outlinerModel, action, actionInspectors);
                
                if (action.Source.Id != 0)
                    ProcessGenericElement(outlinerModel, action.Source, targetInspectors);
                
                if (action.Source.Id != 0)
                    ProcessGenericElement(outlinerModel, action.Target, targetInspectors);
            }
        }
    }
    
    public void ProcessRandomRangeTimedActionList(ISmartScriptOutlinerModel outliner, SmartBaseElement element)
    {
        var min = element.GetParameter(0).Value;
        var max = element.GetParameter(1).Value;
        if (max - min > 12)
            return;

        for (long i = min; i <= max; ++i)
        {
            outliner.Add(SmartScriptType.TimedActionList, (int)i);
        }
    }
    
    private void ProcessGenericElement(ISmartScriptOutlinerModel outlinerModel, SmartBaseElement element, SmartScriptType?[,] types)
    {
        for (int i = 0; i < element.ParametersCount; ++i)
        {
            if (element.Id < 0)
                continue;
            
            var type = types[element.Id, i];
            if (type == null)
                continue;

            var value = element.GetParameter(i).Value;

            if (value <= 0 || value > int.MaxValue)
                continue;

            outlinerModel.Add(type.Value, (int)value);
        }
    }
}