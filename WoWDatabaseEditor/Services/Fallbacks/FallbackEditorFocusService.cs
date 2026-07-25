using System;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.Fallbacks;

[FallbackAutoRegister]
[SingleInstance]
public class FallbackEditorFocusService : IEditorFocusService
{
    public bool IsEditorFocused => true;
    public event Action<bool>? FocusChanged;
}
