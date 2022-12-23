namespace WDE.SmartScriptEditor.Editor.ViewModels;

public class HighlightViewModel
{
    public string Parameter { get; }
    public string Header { get; }
    public int HighlightIndex { get; }
    
    public HighlightViewModel(string header, string parameter, int highlightIndex)
    {
        Parameter = parameter;
        HighlightIndex = highlightIndex;
        Header = header;
    }
}
