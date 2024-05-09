using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Prism.Commands;
using WDE.Common.Database;
using WDE.Common.Services.FindAnywhere;
using WDE.Common.Services.MessageBox;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Conditions.Data;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.Conditions.Services;

[AutoRegister]
public class ConditionsFindAnywhereSource : IFindAnywhereSource
{
    private readonly IConditionDataManager dataManager;
    private readonly IMySqlExecutor mySqlExecutor;
    private readonly Lazy<IMessageBoxService> messageBoxService;

    public ConditionsFindAnywhereSource(IConditionDataManager dataManager,
        IMySqlExecutor mySqlExecutor,
        Lazy<IMessageBoxService> messageBoxService)
    {
        this.dataManager = dataManager;
        this.mySqlExecutor = mySqlExecutor;
        this.messageBoxService = messageBoxService;
    }

    public FindAnywhereSourceType SourceType => FindAnywhereSourceType.Conditions;

    public async Task Find(IFindAnywhereResultContext resultContext, FindAnywhereSourceType searchType, IReadOnlyList<string> parameterNames, long parameterValue, CancellationToken cancellationToken)
    {
        var command = new DelegateCommand(() =>
        {
            messageBoxService.Value.ShowDialog(new MessageBoxFactory<bool>()
                .SetTitle("Operation not supported yet")
                .SetMainInstruction("Conditions not yet supported")
                .SetContent("Sorry, opening conditions directly is not yet supported")
                .WithButton("Sorry again", false, true, true)
                .Build()).ListenErrors();
        });
        var table = Queries.Table(DatabaseTable.WorldTable("conditions"));
        var where = table.ToWhere();
        foreach (var cond in dataManager.AllConditionData)
        {
            if (cond.Parameters == null)
                continue;

            for (int i = 0; i < cond.Parameters.Count; ++i)
            {
                if (cond.Parameters[i].Type == "ConditionObjectEntryParameter")
                {
                    if (parameterNames.IndexOf("CreatureParameter") != -1)
                    {
                        where = where.OrWhere(row => row.Column<int>("ConditionValue1") == 3 &&
                                                     row.Column<long>("ConditionValue3") == parameterValue);
                    }
                    else if (parameterNames.IndexOf("GameobjectParameter") != -1)
                    {
                        where = where.OrWhere(row => row.Column<int>("ConditionValue1") == 5 &&
                                                     row.Column<long>("ConditionValue3") == parameterValue);
                    }
                }
                else if (cond.Parameters[i].Type == "ConditionObjectGuidParameter")
                {
                    if (parameterNames.Any(x => x.StartsWith("CreatureGUID")))
                    {
                        where = where.OrWhere(row => row.Column<int>("ConditionValue1") == 3 &&
                                                     row.Column<long>("ConditionValue3") == parameterValue);
                    }
                    else if (parameterNames.Any(x => x.StartsWith("GameobjectGUID")))
                    {
                        where = where.OrWhere(row => row.Column<int>("ConditionValue1") == 5 &&
                                                     row.Column<long>("ConditionValue3") == parameterValue);
                    }
                }
                else
                {
                    if (parameterNames.IndexOf(cond.Parameters[i].Type) == -1)
                        continue;

                    var colName = "ConditionValue" + (i + 1);

                    where = where.OrWhere(row => row.Column<int>("ConditionTypeOrReference") == cond.Id &&
                                                 row.Column<long>(colName) == parameterValue);   
                }
            }
        }
        
        foreach (var cond in dataManager.AllConditionSourceData)
        {
            if (parameterNames.IndexOf(cond.Group.Type) != -1)
            {
                where = where.OrWhere(row => row.Column<int>("SourceTypeOrReferenceId") == cond.Id &&
                                             row.Column<long>("SourceGroup") == parameterValue);
            }
            if (parameterNames.IndexOf(cond.SourceId.Type) != -1)
            {
                where = where.OrWhere(row => row.Column<int>("SourceTypeOrReferenceId") == cond.Id &&
                                             row.Column<long>("SourceId") == parameterValue);
            }
            if (parameterNames.IndexOf(cond.Entry.Type) != -1)
            {
                where = where.OrWhere(row => row.Column<int>("SourceTypeOrReferenceId") == cond.Id &&
                                             row.Column<long>("SourceEntry") == parameterValue);
            }
        }

        var result = await mySqlExecutor.ExecuteSelectSql(where.Select().QueryString);
        var commentIndex = result.ColumnIndex("Comment");
        foreach (var rowIndex in result)
        {
            resultContext.AddResult(new FindAnywhereResult(new ImageUri("Icons/document_conditions.png"),
                null,
                "Condition " + result.Value(rowIndex, commentIndex),
                 string.Join(", ", Enumerable.Range(0, result.Columns).Select(columnIndex => result.ColumnName(columnIndex) + ": " + result.Value(rowIndex, columnIndex))),
                null,
                command));
        }
    }
}