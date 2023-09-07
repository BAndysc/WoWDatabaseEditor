using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Collections;
using WDE.Module.Attributes;

namespace WDE.Common.TableData;

[UniqueProvider]
public interface ITabularDataPicker
{
    Task<T?> PickRow<T>(ITabularDataArgs<T> args, int defaultSelection = -1, string? defaultSearchText = null) where T : class;
    Task<IReadOnlyCollection<T>?> PickRows<T>(ITabularDataArgs<T> args, IReadOnlyList<int>? defaultSelection = null, string? defaultSearchText = null, bool useCheckBoxes = false)  where T : class;
}

public static class TabularDataPickerExtensions
{
    /// <summary>
    /// PickRow for structs, but be careful: it will box the struct to object every time you all this method
    /// </summary>
    public static async Task<T?> PickStructRow<T>(this ITabularDataPicker self, ITabularDataArgs<T> args, int defaultSelection = -1, string? defaultSearchText = null) where T : struct
    {
        var builder = new TabularDataBuilder<object>(typeof(T))
            .SetColumns(args.Columns)
            .SetTitle(args.Title)
            .SetData(new BoxingIndexedCollection<T>(args.Data))
            .SetFilter((obj, search) => args.FilterPredicate((T)obj, search));
        
        if (args.NumberPredicate != null)
            builder = builder.SetNumberPredicate((obj, num) => args.NumberPredicate((T)obj, num));
        
        if (args.ExactMatchPredicate != null)
            builder = builder.SetExactMatchPredicate((obj, search) => args.ExactMatchPredicate((T)obj, search));
        
        if (args.ExactMatchCreator != null)
            builder = builder.SetExactMatchCreator((search) => args.ExactMatchCreator(search));

        var result = await self.PickRow(builder.Build(), defaultSelection, defaultSearchText);
        var resultTyped = result as T?;
        return resultTyped;
    }
    
    /// <summary>
    /// PickRows for structs, but be careful: it will box the struct to object every time you all this method
    /// </summary>
    public static async Task<IReadOnlyList<T>?> PickStructRows<T>(this ITabularDataPicker self, ITabularDataArgs<T> args, IReadOnlyList<int>? defaultSelection = null, string? defaultSearchText = null, bool useCheckBoxes = false) where T : struct
    {
        var builder = new TabularDataBuilder<object>(typeof(T))
            .SetColumns(args.Columns)
            .SetTitle(args.Title)
            .SetData(new BoxingIndexedCollection<T>(args.Data))
            .SetFilter((obj, search) => args.FilterPredicate((T)obj, search));
        
        if (args.NumberPredicate != null)
            builder = builder.SetNumberPredicate((obj, num) => args.NumberPredicate((T)obj, num));
        
        if (args.ExactMatchPredicate != null)
            builder = builder.SetExactMatchPredicate((obj, search) => args.ExactMatchPredicate((T)obj, search));
        
        if (args.ExactMatchCreator != null)
            builder = builder.SetExactMatchCreator((search) => args.ExactMatchCreator(search));

        var result = await self.PickRows(builder.Build(), defaultSelection, defaultSearchText, useCheckBoxes);
        var resultTyped = result?.Cast<T>().ToList();
        return resultTyped!;
    }

    private class BoxingIndexedCollection<T> : IIndexedCollection<object> where T : struct
    {
        private readonly IIndexedCollection<T> original;

        public BoxingIndexedCollection(IIndexedCollection<T> original)
        {
            this.original = original;
        }

        public object this[int index] => original[index];

        public int Count => original.Count;
        public event Action? OnCountChanged
        {
            add => original.OnCountChanged += value;
            remove => original.OnCountChanged -= value;
        }
    }
}