using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Services.FindAnywhere;
using WDE.Common.Solution;
using WDE.Common.Utils;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Editor;

namespace WDE.SmartScriptEditor.Services;

public abstract class SmartScriptBaseFindAnywhereSource : IFindAnywhereSource
{
    private readonly ISmartDataManager smartDataManager;
    private readonly ISmartScriptDatabaseProvider databaseProvider;
    private readonly IEditorFeatures editorFeatures;
    private readonly ISolutionItemNameRegistry nameRegistry;
    private readonly ISolutionItemIconRegistry iconRegistry;

    public SmartScriptBaseFindAnywhereSource(ISmartDataManager smartDataManager,
        ISmartScriptDatabaseProvider databaseProvider,
        IEditorFeatures editorFeatures,
        ISolutionItemNameRegistry nameRegistry,
        ISolutionItemIconRegistry iconRegistry)
    {
        this.smartDataManager = smartDataManager;
        this.databaseProvider = databaseProvider;
        this.editorFeatures = editorFeatures;
        this.nameRegistry = nameRegistry;
        this.iconRegistry = iconRegistry;
    }

    public int Order => -1;
    
    public FindAnywhereSourceType SourceType => FindAnywhereSourceType.SmartScripts;

    public async Task Find(IFindAnywhereResultContext resultContext, FindAnywhereSourceType searchType, IReadOnlyList<string> parameterNames, long parameterValue,
        CancellationToken cancellationToken)
    {
        HashSet<(int, int)> added = new();
        var results = await databaseProvider.FindSmartScriptLinesBy(PrepareCondition(parameterNames, parameterValue));
        foreach (var result in results)
        {
            if (!added.Add((result.ScriptSourceType, result.EntryOrGuid)))
                continue;
            
            var item = GenerateSolutionItem(result);
            var name = nameRegistry.GetName(item);
            var icon = iconRegistry.GetIcon(item);
            resultContext.AddResult(new FindAnywhereResult(
                icon,
                result.EntryOrGuid,
                name,
                result.Comment,
                item));
        }
    }

    protected abstract ISolutionItem GenerateSolutionItem(ISmartScriptLine line);

    private IEnumerable<(IDatabaseProvider.SmartLinePropertyType what, int whatValue, int parameterIndex, long valueToSearch)> PrepareCondition(IReadOnlyList<string> parameterName, long parameterValue)
    {
        foreach (var thing in PrepareConditionForType(parameterName, parameterValue, SmartType.SmartEvent, IDatabaseProvider.SmartLinePropertyType.Event))
            yield return thing;
        
        foreach (var thing in PrepareConditionForType(parameterName, parameterValue, SmartType.SmartAction, IDatabaseProvider.SmartLinePropertyType.Action))
            yield return thing;
        
        foreach (var thing in PrepareConditionForType(parameterName, parameterValue, SmartType.SmartTarget, IDatabaseProvider.SmartLinePropertyType.Target))
            yield return thing;

        if (editorFeatures.SupportsSource)
        {
            foreach (var thing in PrepareConditionForType(parameterName, parameterValue, SmartType.SmartSource, IDatabaseProvider.SmartLinePropertyType.Source))
                yield return thing;
        }
    }
    
    private IEnumerable<(IDatabaseProvider.SmartLinePropertyType what, int whatValue, int parameterIndex, long valueToSearch)> PrepareConditionForType(IReadOnlyList<string> parameterNames, long parameterValue, SmartType type, IDatabaseProvider.SmartLinePropertyType dbType)
    {
        foreach (var data in smartDataManager.GetAllData(type).Value)
        {
            if (data.Parameters == null)
                continue;
            
            for (var i = 0; i < data.Parameters.Count; i++)
            {
                var parameter = data.Parameters[i];
                if (parameterNames.IndexOf(parameter.Type) != -1)
                    yield return (dbType, data.Id, i + 1, parameterValue);
            }
        }
    }
}