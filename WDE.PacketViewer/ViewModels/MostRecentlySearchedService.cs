using System.Collections.Generic;
using System.Collections.ObjectModel;
using DynamicData;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.PacketViewer.ViewModels
{
    [AutoRegister]
    public class MostRecentlySearchedService
    {
        private readonly IUserSettings userSettings;
        private static int MaxMostRecentlySearched = 15;
        public ObservableCollection<string> mostRecentlySearched = new();
        public ReadOnlyObservableCollection<string> MostRecentlySearched { get; }

        public MostRecentlySearchedService(IUserSettings userSettings)
        {
            this.userSettings = userSettings;
            MostRecentlySearched = new(mostRecentlySearched);
            mostRecentlySearched.AddRange(userSettings.Get(new Data(){MostRecentlySearched = new List<string>()}).MostRecentlySearched);
        }

        public void Add(string search)
        {
            mostRecentlySearched.Remove(search);
            if (mostRecentlySearched.Count > MaxMostRecentlySearched)
                mostRecentlySearched.RemoveAt(mostRecentlySearched.Count - 1);
            
            mostRecentlySearched.Insert(0, search);
            userSettings.Update(new Data(){MostRecentlySearched = mostRecentlySearched});
        }

        private struct Data : ISettings
        {
            public IList<string> MostRecentlySearched { get; set; }
        }
    }
}