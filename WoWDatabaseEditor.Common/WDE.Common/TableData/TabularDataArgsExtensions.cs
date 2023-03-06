using WDE.Common.Collections;
using WDE.Common.Utils;

namespace WDE.Common.TableData;

public static class TabularDataArgsExtensions
{
    public static ITabularDataArgs<object> AsObject<T>(this ITabularDataArgs<T> args)
    {
        return new TabularDataBuilder<object>()
            .SetData((IIndexedCollection<object>)args.Data)
            .SetColumns(args.Columns)
            .SetFilter((x, search) => args.FilterPredicate((T)x, search))
            .SetNumberPredicate(args.NumberPredicate == null ? null : (x, search) => args.NumberPredicate((T)x, search))
            .SetTitle(args.Title)
            .Build();
    }
}