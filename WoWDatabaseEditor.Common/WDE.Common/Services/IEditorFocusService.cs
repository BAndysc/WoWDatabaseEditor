using System;
using WDE.Module.Attributes;

namespace WDE.Common.Services;

/// <summary>
/// Tells whether any of the editor windows currently has the OS focus.
/// </summary>
[UniqueProvider]
public interface IEditorFocusService
{
    bool IsEditorFocused { get; }
    event Action<bool>? FocusChanged;
}
