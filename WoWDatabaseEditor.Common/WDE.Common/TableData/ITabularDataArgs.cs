using System.Collections.Generic;
using WDE.Common.Collections;
using WDE.Common.Utils;

namespace WDE.Common.TableData;

public interface ITabularDataArgs<T>
{
    string Title { get; }
    IReadOnlyList<ITabularDataColumn> Columns { get; }
    System.Func<T, string, bool> FilterPredicate { get; }
    System.Func<T, long, bool>? NumberPredicate { get; }
    IIndexedCollection<T> Data { get; }
}