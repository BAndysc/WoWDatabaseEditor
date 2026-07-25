using System.Collections.Generic;
using System.Linq;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.DbScriptsEditor.Services
{
    [UniqueProvider]
    public interface IFavouriteDbScriptCommandsService
    {
        bool IsFavourite(string name);
        void SetFavourite(string name, bool @is);
    }

    // Persists the user's favourite dbscript commands (starred in the command picker), keyed by
    // the stable SCRIPT_COMMAND_* enum name (plus variant suffix for variant entries).
    [AutoRegister]
    [SingleInstance]
    public class FavouriteDbScriptCommandsService : IFavouriteDbScriptCommandsService
    {
        private readonly IUserSettings userSettings;
        private readonly HashSet<string> favourites;

        public FavouriteDbScriptCommandsService(IUserSettings userSettings)
        {
            this.userSettings = userSettings;
            var data = userSettings.Get<Data>();
            favourites = new(data.Favourites ?? Enumerable.Empty<string>());
        }

        public bool IsFavourite(string name) => favourites.Contains(name);

        public void SetFavourite(string name, bool @is)
        {
            if (@is)
                favourites.Add(name);
            else
                favourites.Remove(name);
            userSettings.Update(new Data { Favourites = favourites.ToList() });
        }

        private struct Data : ISettings
        {
            public List<string>? Favourites { get; set; }
        }
    }
}
