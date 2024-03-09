using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using AvaloniaStyles.Controls;
using FuzzySharp;
using WDE.Common.Parameters;
using WDE.MVVM.Observable;

namespace WDE.Common.Avalonia.Controls;

public class CompletionComboParameterBox : BaseParameterBox
{
    private CompletionComboBox? combo;

    static CompletionComboParameterBox()
    {
        ParameterValueProperty.OverrideMetadata<CompletionComboParameterBox>(new StyledPropertyMetadata<object?>(coerce: CoerceParameterValue));
    }

    private static object? CoerceParameterValue(AvaloniaObject owner, object? arg)
    {
        if (arg is ParameterOption option)
            return option.Value;
        return arg;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        combo = e.NameScope.Get<CompletionComboBox>("PART_ComboBox");
        combo.AsyncPopulator = AsyncPopulator;
        combo.OnEnterPressed += (sender, pressedArgs) =>
        {
            var box = (CompletionComboBox)sender!;
            if (pressedArgs.SelectedItem == null && long.TryParse(pressedArgs.SearchText, out var l))
            {
                SetCurrentValue(ParameterValueProperty, l);
                pressedArgs.Handled = true;
            }
        };
        combo.GetObservable(CompletionComboBox.IsDropDownOpenProperty)
            .SubscribeAction(@is =>
            {
                if (@is && combo.Items == null && Parameter is IParameter<long> p &&
                    p.Items is { } items)
                {
                    combo.Items = items.Select(x => new ParameterOption(x.Key, x.Value.Name))
                        .ToList();
                }
            });
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ParameterProperty)
        {
            if (combo != null)
                combo.Items = null;
        }
    }

    public static Func<IEnumerable, string, CancellationToken, Task<IEnumerable>> AsyncPopulator
    {
        get
        {
            return async (items, s, token) =>
            {
                if (items is not IList o)
                    return Enumerable.Empty<object>();

                if (string.IsNullOrEmpty(s))
                    return items;

                return await Task.Run(() =>
                {
                    bool isNumberSearch = long.TryParse(s, out var number);
                    if (o.Count < 250)
                    {
                        int exactMatchIndex = -1;
                        var result = Process.ExtractSorted(s, items.Cast<object>().Select(item => item.ToString()), cutoff: 51)
                            .Select((item, index) =>
                            {
                                var option = (ParameterOption)o[item.Index]!;
                                if (isNumberSearch && option.Value == number)
                                    exactMatchIndex = index;
                                return o[item.Index]!;
                            }).ToList();
                        if (exactMatchIndex != -1)
                        {
                            var exactMatch = (ParameterOption)result[exactMatchIndex]!;
                            result.RemoveAt(exactMatchIndex);
                            result.Insert(0, exactMatch);
                        }
                        return result;
                    }

                    List<object> picked = new();
                    var search = s.ToLower();
                    foreach (ParameterOption item in o)
                    {
                        if (isNumberSearch && item.Value == number)
                            picked.Insert(0, item);
                        else if (item.ToString()!.Contains(search, StringComparison.InvariantCultureIgnoreCase))
                            picked.Add(item);

                        if (token.IsCancellationRequested)
                            break;
                    }

                    return picked;
                }, token);
            };
        }
    }
}

public class ParameterOption
{
    public ParameterOption(long value, string name)
    {
        Value = value;
        Name = name;
    }

    private string? searchText;
    public long Value { get; }
    public string Name { get; }

    public override string ToString()
    {
        searchText ??= $"{Name} ({Value})";
        return searchText;
    }
}

public class ParameterOptionConverter : IValueConverter
{
    public static ParameterOptionConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ParameterOption option)
            return option.Value.ToString();
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}