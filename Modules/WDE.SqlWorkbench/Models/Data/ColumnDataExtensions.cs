using System;

namespace WDE.SqlWorkbench.Models;

internal static class ColumnDataExtensions
{
    public static string? GetFullToString(this IReadOnlyColumnData columnData, int rowIndex)
    {
        if (columnData.IsNull(rowIndex))
            return null;
        
        if (columnData is BinaryColumnData binary)
        {
            var bytes = binary[rowIndex];
            var str = Convert.ToHexString(bytes);
            return str;
        }
        
        if (columnData is BinarySparseColumnData sparseBinary)
        {
            var bytes = sparseBinary[rowIndex];
            var str = Convert.ToHexString(bytes);
            return str;
        }
        
        return columnData.GetToString(rowIndex);
    }
}