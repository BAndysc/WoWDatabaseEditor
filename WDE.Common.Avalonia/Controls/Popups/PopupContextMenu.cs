using System;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace WDE.Common.Avalonia.Controls.Popups;

public class PopupContextMenu : ContextMenu, IStyleable
{
    Type IStyleable.StyleKey => typeof(ContextMenu);

    public override void Close()
    {
        base.Close();
        this.FindAncestorOfType<PopupMenu>()?.Close();
    }
}