using System;
using System.Threading;
using System.Threading.Tasks;

namespace WDE.Common.TableData;

public interface ITabularDataColumn
{
    public string Header { get; }
    public string PropertyName { get; }
    public float Width { get; }
    public object? DataTemplate { get; }
}

public interface ITabularDataAsyncColumn : ITabularDataColumn
{
    Task<string?> ComputeAsync(object property, CancellationToken token);
}