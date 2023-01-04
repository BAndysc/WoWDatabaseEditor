using System;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using WDE.Common.Types;

namespace WDE.SqlWorkbench.ViewModels;

public class EditorCompletionData : ICompletionData
{
    public EditorCompletionData(string text, string? description, ImageUri icon)
    {
        Text = text;
        CompletionType = description;
        ImageUri = icon;
    }

    public IImage? Image => null;

    public string Text { get; }

    public object? Content => null;

    public object? Description => null;

    public string? CompletionType { get; }

    public double Priority { get; } = 0;

    public ImageUri ImageUri { get; }

    public void Complete(TextArea textArea, ISegment completionSegment,
        EventArgs insertionRequestEventArgs)
    {
        int offset = completionSegment.EndOffset;
        while (offset > 0 && textArea.Document.GetCharAt(offset - 1) is var c &&
               (char.IsLetter(c) || char.IsNumber(c) || c == '_' || c == '`'))
            offset--;
        textArea.Document.Replace(new SimpleSegment(offset, completionSegment.EndOffset - offset), Text);
    }
}