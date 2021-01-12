using System.Windows.Controls;
using System.Windows.Controls.Primitives;

public partial class DarkTheme
{
    private void ListView_Thumb_OnDeltaDrag(object sender, DragDeltaEventArgs e)
    {
        if (!(sender is Thumb thumb))
            return;

        GridViewColumnHeader? parent = thumb.TemplatedParent as GridViewColumnHeader;

        if (parent == null)
            return;

        if (double.IsNaN(parent.Column.Width))
            parent.Column.Width = parent.Column.ActualWidth;

        double x = parent.Column.Width + e.HorizontalChange;
        if (x >= 0)
            parent.Column.Width = x;
    }
}