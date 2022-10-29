using System.Collections.Generic;
using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Editor;

[AutoRegister]
[SingleInstance]
public class SimpleConditionsImporter : ISimpleConditionsImporter
{
    public Dictionary<int, List<SmartCondition>> ImportConditions(SmartScriptBase script, IReadOnlyList<IConditionLine>? conditions)
    {
        Dictionary<int, List<SmartCondition>> conds = new();
        if (conditions != null)
        {
            Dictionary<int, int> prevElseGroupPerLine = new();
            foreach (IConditionLine line in conditions)
            {
                SmartCondition? condition = script.SafeConditionFactory(line);

                if (condition == null)
                    continue;

                if (!conds.ContainsKey(line.SourceGroup - 1))
                    conds[line.SourceGroup - 1] = new List<SmartCondition>();

                if (!prevElseGroupPerLine.TryGetValue(line.SourceGroup - 1, out var prevElseGroup))
                    prevElseGroup = prevElseGroupPerLine[line.SourceGroup - 1] = 0;
                
                if (prevElseGroup != line.ElseGroup && conds[line.SourceGroup - 1].Count > 0)
                {
                    var or = script.SafeConditionFactory(-1);
                    if (or != null)
                        conds[line.SourceGroup - 1].Add(or);
                    prevElseGroupPerLine[line.SourceGroup - 1] = line.ElseGroup;
                }

                conds[line.SourceGroup - 1].Add(condition);
            }   
        }

        return conds;
    }

    public List<SmartCondition> ImportConditions(SmartScriptBase script, IReadOnlyList<ICondition> conditions)
    {
        List<SmartCondition> conds = new();
        int prevElseGroup = 0;
        foreach (ICondition line in conditions)
        {
            SmartCondition? condition = script.SafeConditionFactory(line);

            if (condition == null)
                continue;
            
            if (prevElseGroup != line.ElseGroup && conds.Count > 0)
            {
                var or = script.SafeConditionFactory(-1);
                if (or != null)
                    conds.Add(or);
                prevElseGroup = line.ElseGroup;
            }

            conds.Add(condition);
        }

        return conds;
    }
}