using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Services.FindAnywhere;
using WDE.Common.Solution;
using WDE.Common.Utils;
using WDE.EventAiEditor.Data;
using WDE.EventAiEditor.Editor;

namespace WDE.EventAiEditor.Services;

public abstract class EventAiBaseFindAnywhereSource : IFindAnywhereSource
{
    private readonly IEventAiDataManager eventAiDataManager;
    private readonly IEventAiDatabaseProvider databaseProvider;
    private readonly IEditorFeatures editorFeatures;
    private readonly ISolutionItemNameRegistry nameRegistry;
    private readonly ISolutionItemIconRegistry iconRegistry;

    public EventAiBaseFindAnywhereSource(IEventAiDataManager eventAiDataManager,
        IEventAiDatabaseProvider databaseProvider,
        IEditorFeatures editorFeatures,
        ISolutionItemNameRegistry nameRegistry,
        ISolutionItemIconRegistry iconRegistry)
    {
        this.eventAiDataManager = eventAiDataManager;
        this.databaseProvider = databaseProvider;
        this.editorFeatures = editorFeatures;
        this.nameRegistry = nameRegistry;
        this.iconRegistry = iconRegistry;
    }

    public int Order => -1;
    
    public FindAnywhereSourceType SourceType => FindAnywhereSourceType.EventAi;

    public async Task Find(IFindAnywhereResultContext resultContext, FindAnywhereSourceType searchType, IReadOnlyList<string> parameterNames, long parameterValue,
        CancellationToken cancellationToken)
    {
        var results = await databaseProvider.FindEventAiLinesBy(PrepareCondition(parameterNames, parameterValue));
        foreach (var result in results)
        {
            var item = GenerateSolutionItem(result);
            var name = nameRegistry.GetName(item);
            var icon = iconRegistry.GetIcon(item);
            resultContext.AddResult(new FindAnywhereResult(
                icon,
                result.Id,
                name + " (" + result.CreatureIdOrGuid + ")",
                result.Comment,
                item));
        }
    }

    protected abstract ISolutionItem GenerateSolutionItem(IEventAiLine line);

    private IEnumerable<(EventAiPropertyType what, int whatValue, int parameterIndex, long valueToSearch)> PrepareCondition(IReadOnlyList<string> parameterName, long parameterValue)
    {
        foreach (var propertyType in Enum.GetValues<EventAiPropertyType>())
            foreach (var thing in PrepareConditionForType(parameterName, parameterValue, EventOrAction.Event, propertyType))
                yield return thing;
    }
    
    private IEnumerable<(EventAiPropertyType what, int whatValue, int parameterIndex, long valueToSearch)> PrepareConditionForType(IReadOnlyList<string> parameterNames, long parameterValue, EventOrAction type, EventAiPropertyType dbType)
    {
        foreach (var data in eventAiDataManager.GetAllData(type))
        {
            if (data.Parameters == null)
                continue;
            
            for (var i = 0; i < data.Parameters.Count; i++)
            {
                var parameter = data.Parameters[i];
                if (parameterNames.IndexOf(parameter.Type) != -1)
                    yield return (dbType, (int)data.Id, i + 1, parameterValue);
            }
        }
    }
}