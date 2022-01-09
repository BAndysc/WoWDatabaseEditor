using System.Collections.ObjectModel;

namespace WDE.AnniversaryInfo.ViewModels;

public class CardViewModel
{
    public CardViewModel(string date, params IContentItem[] content)
    {
        Date = date;
        Content.AddRange(content);
    }

    public string Date { get; set; }
    public ObservableCollection<IContentItem> Content { get; } = new();
}