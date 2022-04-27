using System.Collections.Generic;
using System.Linq;
using WDE.Common.Services;

namespace WDE.SmartScriptEditor.Services;

// we are registering it manually in SmartScriptModule in order
// to have one instance of this service globally (for all smart script modules)
public class FavouriteSmartsService : IFavouriteSmartsService
{
    private readonly IUserSettings userSettings;
    private readonly HashSet<string> favourites;

    public FavouriteSmartsService(IUserSettings userSettings)
    {
        this.userSettings = userSettings;
        var data = userSettings.Get<Data>();
        favourites = new(data.Favourites ?? Enumerable.Empty<string>());
    }
    
    public bool IsFavourite(string name)
    {
        return favourites.Contains(name);
    }

    public void SetFavourite(string name, bool @is)
    {
        if (@is)
            favourites.Add(name);
        else
            favourites.Remove(name);
        Save();
    }

    private void Save()
    {
        userSettings.Update(new Data(){Favourites = favourites.ToList()});
    }

    private struct Data : ISettings
    {
        public List<string>? Favourites { get; set; }
    }
}