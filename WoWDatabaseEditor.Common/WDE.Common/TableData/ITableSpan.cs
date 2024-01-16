using System.Collections.Generic;

namespace WDE.Common.TableData;

public interface IReadOnlyTableSpan
{
    (int row, int column) GetFirstCell(int row, int column);
    bool IsMerged(int row, int column);
    bool IsMerged(int row, int column, out int firstRow, out int firstColumn);
}

public interface ITableSpan : IReadOnlyTableSpan
{
    void Merge(int row, int column, int rowSpan, int colSpan);
}

public class TableSpan : ITableSpan
{
    private Dictionary<(int row, int column), (int row, int column)> mergedCells = new();
    
    public (int row, int column) GetFirstCell(int row, int column)
    {
        if (mergedCells.TryGetValue((row, column), out var firstCell))
            return (firstCell.row, firstCell.column);
        return (row, column);
    }

    public bool IsMerged(int row, int column)
    {
        return mergedCells.ContainsKey((row, column));
    }

    public bool IsMerged(int row, int column, out int firstRow, out int firstColumn)
    {
        if (mergedCells.TryGetValue((row, column), out var firstCell))
        {
            firstRow = firstCell.row;
            firstColumn = firstCell.column;
            return true;
        }
        firstRow = row;
        firstColumn = column;
        return false;
    }

    public void Merge(int row, int column, int rowSpan, int colSpan)
    {
        for (int i = 0; i < rowSpan; ++i)
        {
            for (int j = 0; j < colSpan; ++j)
            {
                if (i == 0 && j == 0)
                    continue;
                
                mergedCells[(row + i, column + j)] = (row, column);
            }
        }
    }
}


public interface IGroupedReadOnlyTableSpan
{
    (int row, int column) GetFirstCell(int group, int row, int column);
    bool IsMerged(int group, int row, int column);
    bool IsMerged(int group, int row, int column, out int firstRow, out int firstColumn);
    void GetCellSpan(int group, int row, int column, out int rowSpan, out int colSpan);
}

public interface IGroupedTableSpan : IGroupedReadOnlyTableSpan
{
    void Merge(int group, int row, int column, int rowSpan, int colSpan);
}
