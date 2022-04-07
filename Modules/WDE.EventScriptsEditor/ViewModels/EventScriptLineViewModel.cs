namespace WDE.EventScriptsEditor.ViewModels;

public class EventScriptLineViewModel
{
    public EventScriptLineViewModel(string text, string? comment)
    {
        Text = text;
        Comment = comment;
    }

    public string Text { get; }
    public string? Comment { get; }
    public bool HasComment => !string.IsNullOrEmpty(Comment);
}