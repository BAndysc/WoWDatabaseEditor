using System;
using System.Threading;
using System.Threading.Tasks;

namespace WDE.Common.TableData;

public class TabularDataColumn : ITabularDataColumn
{
    public TabularDataColumn(string propertyName, string header, float width = 100, object? dataTemplate = null)
    {
        PropertyName = propertyName;
        Header = header;
        Width = width;
        DataTemplate = dataTemplate;
    }

    public string Header { get; }
    public string PropertyName { get; }
    public float Width { get; }
    public object? DataTemplate { get; }
}

public class TabularDataAsyncColumn<T> : ITabularDataAsyncColumn
{
    private readonly Func<T, CancellationToken, Task<string?>> compute;

    public TabularDataAsyncColumn(string propertyName, string header, Func<T, CancellationToken, Task<string?>> compute, float width = 100)
    {
        this.compute = compute;
        PropertyName = propertyName;
        Header = header;
        Width = width;
    }

    public string Header { get; }
    public string PropertyName { get; }
    public float Width { get; }
    public object? DataTemplate { get; }
    
    public Task<string?> ComputeAsync(object property, CancellationToken token)
    {
        return compute((T)property, token);
    }
}