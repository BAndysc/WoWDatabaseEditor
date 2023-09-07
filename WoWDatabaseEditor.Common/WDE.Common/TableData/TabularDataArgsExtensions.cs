using System;
using System.Linq;

namespace WDE.Common.TableData;

public static class TabularDataArgsExtensions
{
    public static ITabularDataArgs<object> AsObject<T>(this ITabularDataArgs<T> args)  where T : class
    {
        var exactMatchCreature = args.ExactMatchCreator == null ? null : (Func<string, object?>)(x => args.ExactMatchCreator(x));
        return new TabularDataBuilder<object>(typeof(T))
            .SetData(args.Data)
            .SetColumns(args.Columns)
            .SetFilter((x, search) => args.FilterPredicate((T)x, search))
            .SetNumberPredicate(args.NumberPredicate == null ? null : (x, search) => args.NumberPredicate((T)x, search))
            .SetExactMatchCreator(exactMatchCreature)
            .SetExactMatchPredicate(args.ExactMatchPredicate == null ? null : (x, search) => args.ExactMatchPredicate((T)x, search))
            .SetTitle(args.Title)
            .Build();
    }
}