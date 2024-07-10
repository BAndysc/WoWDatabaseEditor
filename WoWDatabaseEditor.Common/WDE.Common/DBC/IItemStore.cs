using System.Collections.Generic;
using WDE.Common.DBC.Structs;

namespace WDE.Common.DBC;

public interface IItemStore
{
    IReadOnlyList<IItemDisplayInfo> ItemDisplayInfos { get; }
    IItemDisplayInfo? GetItemDisplayInfoById(uint id);

    IReadOnlyList<IDbcItem> Items { get; }
    IDbcItem? GetItemById(uint id);

    IReadOnlyList<IItemModifiedAppearance>? GetItemModifiedAppearances(uint itemId);

    IReadOnlyList<ICurrencyType> CurrencyTypes { get; }
    ICurrencyType? GetCurrencyTypeById(uint id);

    IItemSparse? GetItemSparseById(int id);
    //
    // IReadOnlyList<IItemRandomProperty> RandomProperties { get; }
    // IItemRandomProperty? GetRandomPropertyById(uint id);
    //
    // IReadOnlyList<IItemRandomSuffix> RandomSuffixes { get; }
    // IItemRandomSuffix? GetRandomSuffixById(uint id);
}