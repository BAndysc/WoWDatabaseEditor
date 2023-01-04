using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Utils;
using WDE.Common.Avalonia.Utils;

namespace WDE.Common.Avalonia.Controls.CodeCompletionTextEditor;

public class CodeCompletionTextEditor : TemplatedControl
{
    private CompletionWindow completionWindow = null!;
    private TextEditor? textEditor;
    private TextBlock? placeholder;
    private bool inUpdateTextProperty = false;

    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<CodeCompletionTextEditor, string>(nameof(Text), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string?> PlaceholderTextProperty =
        AvaloniaProperty.Register<CodeCompletionTextEditor, string?>(nameof(PlaceholderText));

    public static readonly StyledProperty<string?> CodeCompletionRootKeyProperty
        = AvaloniaProperty.Register<CodeCompletionTextEditor, string?>(nameof(CodeCompletionRootKey));

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string? PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public string? CodeCompletionRootKey
    {
        get => GetValue(CodeCompletionRootKeyProperty);
        set => SetValue(CodeCompletionRootKeyProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TextProperty)
        {
            if (inUpdateTextProperty)
                return;

            if (textEditor == null)
                return;

            textEditor.Text = Text;
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        textEditor = e.NameScope.Get<TextEditor>("PART_Editor");
        placeholder = e.NameScope.Get<TextBlock>("PART_Placeholder");

        textEditor.Document.UpdateFinished += DocumentUpdateFinished;
        textEditor.TextArea.TextEntered += TextAreaOnTextEntered;
        textEditor.LostFocus += FocusLost;
        textEditor.KeyDown += TextEditorKeyDown;

        textEditor.Text = Text;
        placeholder.IsVisible = string.IsNullOrEmpty(Text);

        completionWindow = new CompletionWindow(textEditor.TextArea);
        completionWindow.Styles.Add(new StyleInclude(new Uri("resm:Styles?assembly=WDE.Common.Avalonia")){Source = new Uri("avares://WDE.Common.Avalonia/Controls/CodeCompletionTextEditor/CodeCompletionTextEditor.axaml")});
        completionWindow.WindowManagerAddShadowHint = true;
        completionWindow.Width = 450;
        completionWindow.MaxHeight = 600;
        completionWindow.IsLightDismissEnabled = true;
        completionWindow.CloseAutomatically = false;
        completionWindow.CloseWhenCaretAtBeginning = false;
    }

    private void TextEditorKeyDown(object? sender, KeyEventArgs e)
    {
    }


    private void FocusLost(object? sender, RoutedEventArgs e)
    {
        inUpdateTextProperty = true;
        SetCurrentValue(TextProperty, textEditor!.Text);
        inUpdateTextProperty = false;
    }

    private void TextAreaOnTextEntered(object? sender, TextInputEventArgs e)
    {
        var autoComplete = ViewBind.ResolveViewModel<ICodeCompletionService>();
        var completions = autoComplete.GetCompletions(CodeCompletionRootKey, textEditor!.Document, textEditor.CaretOffset);

        if (completions == null)
        {
            completionWindow.Hide();
        }
        else
        {
            if (!completionWindow.IsOpen)
            {
                completionWindow.CompletionList.CompletionData.Clear();
                completionWindow.CompletionList.CompletionData.AddRange(completions.Select(x=> new BreakpointLogCompletionViewModel()
                {
                    Text = x.property,
                    Type = x.type
                }));
                completionWindow.Show();
                completionWindow.RefreshCompletion();
            }
        }
    }

    private void DocumentUpdateFinished(object? sender, EventArgs e)
    {
        if (placeholder != null && textEditor != null)
            placeholder.IsVisible = textEditor.Document.TextLength == 0;
    }
}

public class BreakpointLogCompletionViewModel : ICompletionData
{
    public IImage Image => null!;
    public string Text { get; set; } = "";
    public string Type { get; set; } = "";
    public object Content => "";
    public object Description => "";
    public double Priority => 0;

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        int offset = completionSegment.EndOffset;
        while (offset > 0 && textArea.Document.GetCharAt(offset - 1) is var c &&
               (char.IsLetter(c) || char.IsNumber(c)))
            offset--;
        textArea.Document.Replace(new SimpleSegment(offset, completionSegment.EndOffset - offset), Text);
    }
}