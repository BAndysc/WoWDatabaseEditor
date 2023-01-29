using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Avalonia.Media;
using Prism.Commands;
using WDE.AnniversaryInfo.Services;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Services.MessageBox;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.MVVM;

namespace WDE.AnniversaryInfo.ViewModels;

public class TimelineViewModel : ObservableBase, IDocument
{
    private readonly ITimelineProvider timelineProvider;
    private readonly ICommentPublisherService publisherService;
    private readonly IWindowManager windowManager;
    private readonly IMessageBoxService messageBoxService;
    private IImage? bigImage;
    public ICommand Undo => AlwaysDisabledCommand.Command;
    public ICommand Redo => AlwaysDisabledCommand.Command;
    public IHistoryManager? History => null;
    public bool IsModified => false;
    public string Title => "WDE 2021 Summary";
    public ICommand Copy  => AlwaysDisabledCommand.Command;
    public ICommand Cut  => AlwaysDisabledCommand.Command;
    public ICommand Paste  => AlwaysDisabledCommand.Command;
    public IAsyncCommand Save  => AlwaysDisabledAsyncCommand.Command;
    public IAsyncCommand? CloseCommand { get; set; }
    public bool CanClose => true;
    public ImageUri? Icon => new ImageUri("Icons/document_party.png");

    public ObservableCollection<CardViewModel> Cards { get; } = new();

    public IImage? BigImage
    {
        get => bigImage;
        set => SetProperty(ref bigImage, value);
    }

    public DelegateCommand<IImage?> OpenBigImage { get; }
    public ICommand CloseBigImage { get; }
    
    public TimelineViewModel(ITimelineProvider timelineProvider,
        ICommentPublisherService publisherService,
        IWindowManager windowManager,
        IMessageBoxService messageBoxService)
    {
        this.timelineProvider = timelineProvider;
        this.publisherService = publisherService;
        this.windowManager = windowManager;
        this.messageBoxService = messageBoxService;

        Cards.Add(new CardViewModel("WoW Database Editor :: 2021 Summary", 
            new TextContentItem("2021 has been a busy year, no doubt. But did you know that WoW Database Editor is already 5 years old?! And in fact, the development of it begun in 2015?"),
            new TextContentItem("Fasten your belts and take a look at history of the development!"),
            new TextContentItem(""),
            new TextContentItem("(NOTE: things here are downloaded from the Internet, if nothing appears here, it means there is some internet connection problem)"),
            new ButtonContentItem("Open web version", new DelegateCommand(timelineProvider.OpenWeb))));
        
        Cards.Add(new CardViewModel("2015", new TextContentItem("(loading)")));
        
        Load().ListenErrors();

        OpenBigImage = new DelegateCommand<IImage?>(img => BigImage = img);

        CloseBigImage = new DelegateCommand(() => BigImage = null);
    }

    private async Task Load()
    {
        var http = new HttpClient();
        var data = await timelineProvider.GetTimeline();
        List<Func<Task>> toLoad = new();
        
        Cards.Clear();
        
        foreach (var item in data)
        {
            var content = new List<IContentItem>();
            foreach (var c in item.Content)
            {
                if (c.StartsWith("img:"))
                {
                    var image = new ImageContentItem();
                    toLoad.Add(() => image.AsyncLoad(c.Substring(4), http));
                    content.Add(image);
                }
                else if (c.StartsWith("link:"))
                {
                    var parts = c.Substring(5).Split('|');
                    if (parts.Length == 2)
                    {
                        var link = new ButtonContentItem(parts[0], new DelegateCommand(() => windowManager.OpenUrl(parts[1])));
                        content.Add(link);
                    }
                }
                else if (c.StartsWith("guestform:"))
                {
                    content.Add(new GuestBookFormContentItem(publisherService, messageBoxService));
                }
                else
                {
                    var text = new TextContentItem(c);
                    content.Add(text);
                }
            }
            Cards.Add(new CardViewModel(item.Date, content.ToArray()));
        }
        foreach (var item in toLoad)
        {
            await item();
        }
    }
}