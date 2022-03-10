namespace AvaloniaStyles.Controls.FastTableView;

public class WdeCellInteractor : ICustomCellInteractor
{
    public bool PointerDown(ITableCell cell, bool leftButton, bool rightButton, int clickCount)
    {
        if (cell is not WdeCell wde)
            return false;

        if (clickCount == 2 && wde.Value is bool)
            return true;
        
        if (clickCount == 2 && wde.Value is System.Action)
            return true;
        
        return false;
    }

    public bool PointerUp(ITableCell cell, bool leftButton, bool rightButton)
    {
        if (cell is not WdeCell wde)
            return false;

        if (leftButton && wde.Value is bool b)
        {
            wde.Value = !b;
            return true;
        }
        if (leftButton && wde.Value is System.Action action)
        {
            action();
            return true;
        }

        return false;
    }
}