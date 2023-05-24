using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Collections;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Services;
using WDE.Common.TableData;
using WDE.Common.Utils;

namespace WDE.DatabaseEditors.Parameters;

public class BroadcastTextOnlyPickerParameter : IParameter<long>, ICustomPickerParameter<long>
{
    private readonly IDatabaseProvider databaseProvider;
    private readonly ITabularDataPicker tabularDataPicker;

    public BroadcastTextOnlyPickerParameter(IDatabaseProvider databaseProvider,
        ITabularDataPicker tabularDataPicker)
    {
        this.databaseProvider = databaseProvider;
        this.tabularDataPicker = tabularDataPicker;
    }

    public async Task<(long, bool)> PickValue(long value)
    {
        var texts = await databaseProvider.GetBroadcastTextsAsync();
        var selected = await tabularDataPicker.PickRow(new TabularDataBuilder<IBroadcastText>()
            .SetTitle("Pick broadcast text")
            .SetData(texts.AsIndexedCollection())
            .SetColumns(new TabularDataColumn(nameof(IBroadcastText.Id), "Id", 65),
                new TabularDataColumn(nameof(IBroadcastText.Text), "Male Text", 200),
                new TabularDataColumn(nameof(IBroadcastText.Text1), "Female Text", 200))
            .SetFilter((text, search) =>
            {
                var idHas = text.Id.Contains(search);
                var maleHas = text.Text?.Contains(search, StringComparison.InvariantCultureIgnoreCase) ?? false;
                var femaleHas = text.Text1?.Contains(search, StringComparison.InvariantCultureIgnoreCase) ?? false;
                return idHas || maleHas || femaleHas;
            })
            .SetExactMatchPredicate((template, search) => template.Id.Is(search))
            .SetExactMatchCreator(search =>
            {
                if (!uint.TryParse(search, out var entry))
                    return null;
                return new AbstractBroadcastText()
                {
                    Id = entry,
                    Text = "Pick non existing"
                };
            })
            .Build(), defaultSearchText: value > 0 ? value.ToString() : null);
        
        if (selected == null)
            return (0, false);

        return (selected.Id, true);
    }

    public string? Prefix => null;
    public bool HasItems => true;
    public bool AllowUnknownItems => true;

    public string ToString(long value) => value.ToString();

    public Dictionary<long, SelectOption>? Items => null;
}

public class BroadcastTextParameter : BroadcastTextOnlyPickerParameter, IAsyncParameter<long>
{
    private readonly IDatabaseProvider databaseProvider;
    private readonly ITabularDataPicker tabularDataPicker;

    public BroadcastTextParameter(IDatabaseProvider databaseProvider,
        ITabularDataPicker tabularDataPicker) : base (databaseProvider, tabularDataPicker)
    {
        this.databaseProvider = databaseProvider;
        this.tabularDataPicker = tabularDataPicker;
    }

    public async Task<string> ToStringAsync(long val, CancellationToken token)
    {
        if (val >= uint.MaxValue || val <= 0)
            return ToString(val);
        
        var result = await databaseProvider.GetBroadcastTextByIdAsync((uint)val);
        var text = (string.IsNullOrEmpty(result?.Text) ? result?.Text1 : result?.Text);
        if (text == null || result == null)
            return ToString(val);

        return $"{text.TrimToLength(60)} ({val})";
    }
}