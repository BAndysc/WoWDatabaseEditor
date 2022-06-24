using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WDE.AnniversaryInfo.ViewModels;
using WDE.Common.Managers;
using WDE.Module.Attributes;

namespace WDE.AnniversaryInfo.Services;

[AutoRegister]
public class TimelineProvider : ITimelineProvider
{
    private readonly IWindowManager windowManager;
    private static string baseUrl = "https://bandysc.github.io/WoWDatabaseEditor/2021/";
    private static string sourceUrl = baseUrl + "timeline.json";

    public TimelineProvider(IWindowManager windowManager)
    {
        this.windowManager = windowManager;
    }
    
    public async Task<IList<TimeLineItem>> GetTimeline()
    {
        var http = new HttpClient();
        try
        {
            var response = await http.GetStringAsync(sourceUrl);
            var items = JsonConvert.DeserializeObject<List<TimeLineItem>>(response)!
                .Where(item => !item.Skip)
                .ToList();
            
            foreach (var item in items)
            {
                for (int i = 0; i < item.Content.Count; ++i)
                {
                    if (item.Content[i].StartsWith("img:") && !item.Content[i].AsSpan(4).StartsWith("http"))
                    {
                        item.Content[i] = "img:" + baseUrl + item.Content[i].Substring(4);
                    }
                }
            }

            return items;
        }
        catch (Exception e)
        {
            return new List<TimeLineItem>()
            {
                new()
                {
                    Date = "Error while downloading data",
                    Content = new List<string>()
                    {
                        "Sorry, couldn't load the timeline due to an error. Please try again later.",
                        e.Message
                    }
                }
            };
        }
    }

    public void OpenWeb()
    {
        windowManager.OpenUrl(baseUrl);
    }
}