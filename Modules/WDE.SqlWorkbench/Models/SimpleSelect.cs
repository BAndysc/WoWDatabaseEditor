using System.Collections.Generic;

namespace WDE.SqlWorkbench.Models;

internal readonly struct SimpleSelect
{
    public readonly string SelectItems;
    public readonly SimpleFrom From;
    public readonly string? Where;
    public readonly string? Order;
    public readonly string? Limit;

    public SimpleSelect(string selectItems, SimpleFrom from, string? where, string? order, string? limit)
    {
        SelectItems = selectItems;
        From = from;
        Where = where;
        Order = order;
        Limit = limit;
    }
    
    public SimpleSelect WithWhere(string where)
    {
        return new(SelectItems, From, where, Order, Limit);
    }
    
    public SimpleSelect WithOrder(string order)
    {
        return new(SelectItems, From, Where, order, Limit);
    }
    
    public override string ToString()
    {
        List<string> tokens = new() { "SELECT", SelectItems, "FROM", From.ToString() };
        if (Where != null)
        {
            tokens.Add("WHERE");
            tokens.Add(Where);
        }
        if (Order != null)
        {
            tokens.Add("ORDER BY");
            tokens.Add(Order);
        }
        if (Limit != null)
        {
            tokens.Add("LIMIT");
            tokens.Add(Limit);
        }
        return string.Join(" ", tokens);
    }
}