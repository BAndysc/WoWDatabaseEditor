using System;
using WDE.AnniversaryInfo.ViewModels;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.AnniversaryInfo.Services;

[SingleInstance]
[AutoRegister]
public class AnniversarySummaryService : IAnniversarySummaryService
{
    private readonly IDocumentManager documentManager;
    private readonly Func<TimelineViewModel> timelineViewModelFactory;
    private readonly IUserSettings userSettings;
    private Data data;

    public AnniversarySummaryService(IDocumentManager documentManager, 
        Func<TimelineViewModel> timelineViewModelFactory,
        IUserSettings userSettings)
    {
        this.documentManager = documentManager;
        this.timelineViewModelFactory = timelineViewModelFactory;
        this.userSettings = userSettings;
        data = userSettings.Get<Data>();
        ShowAnniversaryBox = false;
    }
    
    public void OpenSummary()
    {
        documentManager.OpenDocument(timelineViewModelFactory());
    }

    public void TryOpenDefaultSummary()
    {
        // disabled
        // if (!data.SummaryAutoShown)
        // {
        //     data.SummaryAutoShown = true;
        //     userSettings.Update(data);
        //     OpenSummary();
        // }
    }

    public bool ShowAnniversaryBox { get; set; }
    
    public void HideAnniversaryBox()
    {
        ShowAnniversaryBox = false;
        data.HideSummaryBox = true;
        userSettings.Update(data);
    }

    public struct Data : ISettings
    {
        public bool HideSummaryBox { get; set; }
        public bool SummaryAutoShown { get; set; }
    }
}