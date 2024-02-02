using System.Collections.Generic;
using AvaloniaEdit.Document;
using WDE.Module.Attributes;

namespace WDE.Common.Avalonia.Controls.CodeCompletionTextEditor;

[UniqueProvider]
public interface ICodeCompletionService
{
    IReadOnlyList<(string property, string type)>? GetCompletions(string? rootKey, ITextSource str, int position);
}