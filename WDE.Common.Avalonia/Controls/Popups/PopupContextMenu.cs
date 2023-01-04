using System;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace WDE.Common.Avalonia.Controls.Popups;

public class PopupContextMenu : ContextMenu
{
    protected override Type StyleKeyOverride => typeof(ContextMenu);

    public override void Close()
    {
        base.Close();
        this.FindAncestorOfType<PopupMenu>()?.Close();
    }
}