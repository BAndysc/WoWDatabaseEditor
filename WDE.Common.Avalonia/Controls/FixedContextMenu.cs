using System;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Diagnostics;
using Avalonia.Controls.Primitives;
using Avalonia.Styling;

namespace WDE.Common.Avalonia.Controls;

/// <summary>
/// Avalonia has a bug: a Context menu will always keep a reference to the previous focused control, even if the menu is closed.
/// which creates massive leaks.
/// </summary>
public class FixedContextMenu : ContextMenu
{
    protected override Type StyleKeyOverride => typeof(ContextMenu);
}