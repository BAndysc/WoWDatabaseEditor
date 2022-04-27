namespace WDE.Common.Services.QueryParser.Models;

public class WhereCondition
{
    public WhereCondition(EqualityWhereCondition[] conditions)
    {
        Conditions = conditions;
    }

    public WhereCondition(EqualityWhereCondition condition)
    {   
        Conditions = new[]{condition};
    }

    public EqualityWhereCondition[] Conditions { get; }
}