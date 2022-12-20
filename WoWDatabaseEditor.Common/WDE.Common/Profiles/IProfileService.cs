using System;
using System.Collections.Generic;
using System.Reactive;
using System.Threading.Tasks;
using WDE.Common.CoreVersion;
using WDE.Common.Database;

namespace WDE.Common.Profiles;

public interface IProfileService
{
    Task<ProfilesData> GetProfiles();
    Task<IList<RunningProfile>> GetRunningProfiles();
    ICoreVersion? GetCoreVersionForProfile(string key);
    Task RenameProfile(string key, string newName);
    Task DeleteProfile(string key);
    Task CreateProfile(string newName, ICoreVersion coreVersion, bool makeDefault, double hue);
    void StartProfile(string key, ISolutionItem? openItem = null);
    void SwitchToInstance(RunningProfile profileInstance);
    Task<bool> TryOpenItemInCore(string coreTag, ISolutionItem item);
    Task SetDefaultProfile(string key);
    Profile GetCurrentProfile();
    string CurrentProfileKey { get; }
}

public class ProfilesData
{
    public List<Profile> Profiles { get; set; } = new List<Profile>();
    public string? DefaultProfileKey { get; set; } = "";
}

public class Profile
{
    public readonly string Key;
    public readonly string Name;

    public Profile(string key, string name)
    {
        Key = key;
        Name = name;
    }
}

public readonly struct RunningProfile
{
    public readonly string Key;
    public readonly int Port;

    public RunningProfile(string key, int port)
    {
        Key = key;
        Port = port;
    }
}