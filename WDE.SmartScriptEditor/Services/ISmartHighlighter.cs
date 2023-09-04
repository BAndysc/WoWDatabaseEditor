using System.Collections.Generic;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor.Editor.ViewModels;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Services;

[UniqueProvider]
public interface ISmartHighlighter
{
    void SetHighlightParameter(string? parameter);
    bool Highlight(SmartEvent e, out int parameterIndex);
    bool Highlight(SmartAction a, out int parameterIndex);
    bool Highlight(SmartCondition c, out int parameterIndex);
    IReadOnlyList<HighlightViewModel> Highlighters { get; }
}