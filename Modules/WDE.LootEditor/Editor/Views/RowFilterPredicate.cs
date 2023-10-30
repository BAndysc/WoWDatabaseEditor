using System;
using AvaloniaStyles.Controls.FastTableView;
using WDE.Common.Utils;
using WDE.LootEditor.Editor.ViewModels;

namespace WDE.LootEditor.Editor.Views;

public class RowFilterPredicate : IRowFilterPredicate
{
    public bool IsVisible(ITableRow row, object? searchTextObj)
    {
        if (searchTextObj is not string searchText ||
            string.IsNullOrWhiteSpace(searchText))
            return true;

        if (row is not LootItemViewModel option)
            return true;

        long? searchTextNum = null;
        if (long.TryParse(searchText, out var searchTextLong))
            searchTextNum = searchTextLong;

        foreach (var cell in option.CellsList)
        {
            if (searchTextNum.HasValue)
            {
                if (cell is not LootItemParameterCell<long> longCell)
                    continue;

                if (longCell.Value.Contains(searchText))
                    return true;
            }
            else
            {
                if (cell.ToString() is not { } value)
                    continue;

                if (value.Contains(searchText, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
        }

        return false;
    }
}