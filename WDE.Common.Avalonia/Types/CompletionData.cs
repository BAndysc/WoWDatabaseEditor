using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;

namespace WDE.Common.Avalonia.Types;

public class CompletionData : ICompletionData
{
    public CompletionData(string text, string? subText = null)
    {
        Text = text;
        SubText = subText;
    }

    public IImage? Image => null;

    public string Text { get; }
    
    public string? SubText { get; }

    // Use this property if you want to show a fancy UIElement in the list.
    public object Content => new DockPanel()
    {
        Children =
        {
            new TextBlock() { Text = Text },
            new TextBlock() { Text = SubText, Opacity = 0.5f, Margin = new Thickness(15,0,0,0) },
        }
    };

    public object? Description => null;

    public double Priority { get; } = 0;

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        textArea.Document.Replace(completionSegment, Text);
    }
}