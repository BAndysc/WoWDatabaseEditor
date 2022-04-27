using Prism.Mvvm;

namespace WDE.AnniversaryInfo.ViewModels;

public class TextContentItem : BindableBase, IContentItem
{
    public TextContentItem(string text)
    {
        Text = text;
    }

    public string Text { get; }
}