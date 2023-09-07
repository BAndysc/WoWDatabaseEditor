using System;
using System.Collections.Generic;
using WDE.Common.Collections;
using WDE.Common.Utils;

namespace WDE.Common.TableData;

public class TabularDataBuilder<T> : ITabularDataArgs<T> 
{
    public string Title { get; private set; } = null!;
    public System.Func<T, string, bool> FilterPredicate { get; private set; } = (_, _) => false;
    public System.Func<T, long, bool>? NumberPredicate { get; private set; }
    public Func<T, string, bool>? ExactMatchPredicate { get; private set; }
    public Func<string, T?>? ExactMatchCreator { get; private set; }
    public IIndexedCollection<T> Data { get; private set; } = new EmptyIndexedCollection<T>();
    public Type Type { get; } = typeof(T);
    public IReadOnlyList<ITabularDataColumn> Columns { get; private set; } = Array.Empty<ITabularDataColumn>();

    public TabularDataBuilder(System.Type? type = null)
    {
        if (type != null)
            Type = type;
    }
    
    public TabularDataBuilder<T> SetTitle(string title)
    {
        Title = title;
        return this;
    }
    
    public TabularDataBuilder<T> SetFilter(System.Func<T, string, bool> filterPredicate)
    {
        FilterPredicate = filterPredicate;
        return this;
    }
    
    public TabularDataBuilder<T> SetNumberPredicate(System.Func<T, long, bool>? numberPredicate)
    {
        NumberPredicate = numberPredicate;
        return this;
    }
    
    public TabularDataBuilder<T> SetData(IIndexedCollection<T> data)
    {
        Data = data;
        return this;
    }
    
    public TabularDataBuilder<T> SetColumns(IReadOnlyList<ITabularDataColumn> columns)
    {
        Columns = columns;
        return this;
    }
    
    public TabularDataBuilder<T> SetColumns(params ITabularDataColumn[] columns)
    {
        Columns = columns;
        return this;
    }
    
    public TabularDataBuilder<T> SetExactMatchPredicate(Func<T, string, bool>? exactMatchPredicate)
    {
        ExactMatchPredicate = exactMatchPredicate;
        return this;
    }
    
    public TabularDataBuilder<T> SetExactMatchCreator(Func<string, T?>? exactMatchCreator)
    {
        ExactMatchCreator = exactMatchCreator;
        return this;
    }

    public ITabularDataArgs<T> Build()
    {
        if (Columns.Count == 0)
            throw new Exception("No columns defined");
        return this;
    }
}