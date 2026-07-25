using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Profiles;
using WDE.Module.Attributes;

namespace LoaderAvalonia.Web;

[AutoRegister]
public class WebProfileService : IProfileService
{
    ICoreVersion? cmangosCore;

    public WebProfileService(IEnumerable<ICoreVersion> coreVersions)
    {
        cmangosCore = coreVersions.FirstOrDefault(x => x.Tag == "CMaNGOS-WoTLK");
    }

    public async Task<ProfilesData> GetProfiles()
    {
        return new ProfilesData()
        {
            DefaultProfileKey = "CMangos",
            Profiles =
            [
                new Profile("CMangos", "CMangos")
            ]
        };
    }

    public async Task<IList<RunningProfile>> GetRunningProfiles()
    {
        return [];
    }

    public ICoreVersion? GetCoreVersionForProfile(string key)
    {
        return cmangosCore;
    }

    public async Task RenameProfile(string key, string newName)
    {
    }

    public async Task DeleteProfile(string key)
    {
    }

    public async Task CreateProfile(string newName, ICoreVersion coreVersion, bool makeDefault, double hue)
    {
    }

    public void StartProfile(string key, ISolutionItem? openItem = null)
    {
    }

    public void SwitchToInstance(RunningProfile profileInstance)
    {
    }

    public async Task<bool> TryOpenItemInCore(string coreTag, ISolutionItem item)
    {
        return false;
    }

    public async Task SetDefaultProfile(string key)
    {
    }

    public Profile GetCurrentProfile()
    {
        return new Profile("CMangos", "CMangos");
    }

    public string CurrentProfileKey => "CMangos";
}