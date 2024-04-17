using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Profiles;
using WDE.Common.Services.MessageBox;
using WDE.Common.Services.Processes;
using WDE.Common.Solution;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WDE.Profiles.Services;

[AutoRegister]
[SingleInstance]
public class ProfileService : IProfileService
{
    private const string CoreConfigFileName = "WoWDatabaseEditorCore.CoreVersion.CurrentCoreSettings.Data.json";
    private const string FirstTimeWizardConfigFileName = "WDE.FirstTimeWizard.Services.FirstTimeWizardSettings.Data.json";
    private const string AppearanceConfigFileName = "WoWDatabaseEditorCore.Avalonia.Services.AppearanceService.Data.ThemeSettings.json";
    private const string FirstTimeWizardConfig = "{\"state\": 2}"; // show first time config wizard

    private readonly IProfilesFileSystem fileSystem;
    private readonly IInterEditorCommunication interEditorCommunication;
    private readonly IProcessService processService;
    private readonly IProjectItemSerializer serializer;
    private readonly ISolutionItemSerializerRegistry serializerRegistry;
    private readonly IMessageBoxService messageBoxService;
    private readonly List<ICoreVersion> coreVersions;
    private readonly Dictionary<string, ICoreVersion> coreVersionsByKey;
    private string currentProfileKey;
    
    public string CurrentProfileKey => currentProfileKey;
    
    public ProfileService(IProfilesFileSystem fileSystem,
        IInterEditorCommunication interEditorCommunication,
        IProcessService processService,
        IProjectItemSerializer serializer,
        ISolutionItemSerializerRegistry serializerRegistry,
        IMessageBoxService messageBoxService,
        IEnumerable<ICoreVersion> coreVersions)
    {
        this.fileSystem = fileSystem;
        this.interEditorCommunication = interEditorCommunication;
        this.processService = processService;
        this.serializer = serializer;
        this.serializerRegistry = serializerRegistry;
        this.messageBoxService = messageBoxService;
        this.coreVersions = coreVersions.ToList();
        coreVersionsByKey = this.coreVersions.ToDictionary(x => x.Tag, x => x);
        
        currentProfileKey = ReadDefaultProfileKey();
    }

    public static string ReadDefaultProfileKey()
    {
        if (GlobalApplication.Arguments.IsArgumentSet("profile") &&
            GlobalApplication.Arguments.GetValue("profile") != null)
            return GlobalApplication.Arguments.GetValue("profile")!;
        else if (TryReadProfiles(out var profileData) && !string.IsNullOrEmpty(profileData.DefaultProfileKey))
            return profileData.DefaultProfileKey;
        else if (File.Exists("profile"))
            return File.ReadAllText("profile");
        else
            return "default";
    }

    private static bool TryReadProfiles(out ProfilesData profiles)
    {
        profiles = null!;
        
        var localDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var rootPath = Path.Join(localDataPath, "WoWDatabaseEditor");
        var rootCommonPath = Path.Join(rootPath, "common");
        var profilesPath = Path.Join(rootCommonPath, "profiles.json");

        if (!File.Exists(profilesPath))
            return false;

        try
        {
            var json = File.ReadAllText(profilesPath);
            var data  = JsonConvert.DeserializeObject<ProfilesData>(json);
            if (data == null)
                return false;
            
            profiles = data;

            return true;
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return false;
        }
    }

    private async Task SaveProfiles(ProfilesData profiles)
    {
        var file = fileSystem.GetFilePath("profiles.json");
        var json = JsonConvert.SerializeObject(profiles);
        await RetryTask(() => File.WriteAllTextAsync(file.FullName, json), TimeSpan.FromSeconds(1), 5);
    }

    private async Task<bool> RetryTask(Func<Task> task, TimeSpan delay, int retries = 1)
    {
        for (int i = 0; i < retries; ++i)
        {
            try
            {
                await task();
                return true;
            }
            catch (Exception e)
            {
                LOG.LogError(e);
            }

            await Task.Delay(delay);
        }

        return false;
    }

    private ProfilesData GetProfilesSync()
    {
        ProfilesData? profiles = null;
        try
        {
            var file = fileSystem.GetFilePath("profiles.json");
            if (file.Exists)
            {
                var json = File.ReadAllText(file.FullName);
                profiles = JsonConvert.DeserializeObject<ProfilesData>(json);
            }
        }
        catch (Exception e)
        {
            LOG.LogError(e);
        }
        
        if (profiles == null)
            profiles = new ProfilesData() { DefaultProfileKey = currentProfileKey, Profiles = new List<Profile>() };
            
        if (profiles.Profiles.FindIndex(x => x.Key == currentProfileKey) == -1)
        {
            profiles.Profiles.Add(new Profile(currentProfileKey, currentProfileKey));
        }
        return profiles;
    }
    
    public async Task<ProfilesData> GetProfiles()
    {
        ProfilesData? profiles = null;
        try
        {
            var file = fileSystem.GetFilePath("profiles.json");
            if (file.Exists)
            {
                var json = await fileSystem.ReadAllText("profiles.json");
                profiles = JsonConvert.DeserializeObject<ProfilesData>(json);
            }
        }
        catch (Exception e)
        {
            LOG.LogError(e);
        }
        
        if (profiles == null)
            profiles = new ProfilesData();
        if (profiles.Profiles.FindIndex(x => x.Key == currentProfileKey) == -1)
        {
            profiles.Profiles.Add(new Profile(currentProfileKey, currentProfileKey));
        }
        return profiles;
    }

    public async Task<IList<RunningProfile>> GetRunningProfiles()
    {
        return await interEditorCommunication.GetRunning();
    }
    
    public ICoreVersion? GetCoreVersionForProfile(string key)
    {
        try
        {
            var settings = fileSystem.GetProfileFilePath(key, CoreConfigFileName);
            if (!settings.Exists)
                return null;

            var content = File.ReadAllText(settings.FullName);
            var coreSettings = JsonConvert.DeserializeObject<CoreVersionSettings>(content);
        
            if (coreSettings.Version != null && 
                coreVersionsByKey.TryGetValue(coreSettings.Version, out var coreVersion))
                return coreVersion;

            return null;
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return null;
        }
    }

    public async Task RenameProfile(string key, string newName)
    {
        var profiles = (await GetProfiles());
        var index = profiles.Profiles.IndexIf(x => x.Key == key);
        if (index == -1)
            return;
        
        profiles.Profiles[index] = new Profile(key, newName);
        await SaveProfiles(profiles);
    }

    public async Task DeleteProfile(string key)
    {
        var profiles = (await GetProfiles());
        var index = profiles.Profiles.IndexIf(x => x.Key == key);
        if (index == -1)
            return;
        
        profiles.Profiles.RemoveAt(index);
        await SaveProfiles(profiles);
        
        var profileDirectory = fileSystem.GetProfileDirectoryPath(key);
        
        if (profileDirectory.Exists)
            profileDirectory.Delete(true);
    }
    
    public async Task CreateProfile(string newName, ICoreVersion coreVersion, bool makeDefault, double hue)
    {
        var key = new string(newName.Where(char.IsLetterOrDigit).ToArray());
        if (string.IsNullOrEmpty(key))
            key = Guid.NewGuid().ToString("N");
     
        var profiles = (await GetProfiles());
        
        var originalKey = key;
        int index = 0;
        while (true)
        {
            var indexOf = profiles.Profiles.IndexIf(x => x.Key == key);
            if (indexOf == -1)
                break;
            
            key = originalKey + (index++).ToString();
        }
        
        profiles.Profiles.Add(new Profile(key, newName));
        if (makeDefault)
            profiles.DefaultProfileKey = key;
        
        await SaveProfiles(profiles);
        
        var profileDirectory = fileSystem.GetProfileDirectoryPath(key);
        if (!profileDirectory.Exists)
            profileDirectory.Create();

        CoreVersionSettings setting = new CoreVersionSettings()
        {
            Version = coreVersion.Tag
        };
        var json = JsonConvert.SerializeObject(setting);
        await File.WriteAllTextAsync(fileSystem.GetProfileFilePath(key, CoreConfigFileName).FullName, json);

        ThemesSettings themesSettings = new ThemesSettings()
        {
            Hue = hue
        };
        json = JsonConvert.SerializeObject(themesSettings);
        await File.WriteAllTextAsync(fileSystem.GetProfileFilePath(key, AppearanceConfigFileName).FullName, json);

        var firstTimePath = fileSystem.GetProfileFilePath(key, FirstTimeWizardConfigFileName);
        if (!firstTimePath.Exists)
            await File.WriteAllTextAsync(firstTimePath.FullName, FirstTimeWizardConfig);
    }

    public void StartProfile(string key, ISolutionItem? item = null)
    {
        var exe = FindExecutable();
        if (!File.Exists(exe))
            return;

        string additionalArgs = "";
        if (item != null)
        {
            var projectItem = serializerRegistry.Serialize(item, true);
            if (projectItem != null)
            {
                var serialized = serializer.Serialize(projectItem);
                additionalArgs = "--open \"" + serialized.Replace("\"", "\"\"\"") +  "\"";
            }
        }

        Process.Start(new ProcessStartInfo(exe, $"--profile {key} {additionalArgs}")
        {
            UseShellExecute = true,
            WorkingDirectory = Environment.CurrentDirectory,
            CreateNoWindow = false
        });
        //processService.RunAndForget(exe, $"--profile {key}", Environment.CurrentDirectory, false);
    }
    

    private static string? FindExecutable()
    {       
        if (File.Exists("WoWDatabaseEditorCore.Avalonia.exe"))
            return "WoWDatabaseEditorCore.Avalonia.exe";
            
        if (File.Exists("WoWDatabaseEditorCore.Avalonia"))
            return "WoWDatabaseEditorCore.Avalonia";

        return null;
    }

    public void SwitchToInstance(RunningProfile profileInstance)
    {
        interEditorCommunication.RequestActivate(profileInstance);
    }

    public async Task<bool> TryOpenItemInCore(string coreTag, ISolutionItem item)
    {
        var running = await interEditorCommunication.GetRunning();
        var foundProfile = running
            .Select(p => (RunningProfile?)p)
            .FirstOrDefault(profile => GetCoreVersionForProfile(profile!.Value.Key)?.Tag == coreTag);

        if (foundProfile.HasValue)
        {
            var projectItem = serializerRegistry.Serialize(item, true);
            if (projectItem != null)
                interEditorCommunication.RequestOpen(foundProfile.Value, projectItem);
            return true;
        }
        else
        {
            var profiles = await GetProfiles();
            var foundConfiguration = profiles.Profiles.FirstOrDefault(p => GetCoreVersionForProfile(p.Key)?.Tag == coreTag);
            if (foundConfiguration != null)
            {
                StartProfile(foundConfiguration.Key, item);
                return true;
            }
        }
        
        await messageBoxService.SimpleDialog("No profile found",
            "No profile with " + coreTag + " core version found",
            "Create a new profile with this core version tag, in order to start this editor directly.");

        return false;
    }

    public async Task SetDefaultProfile(string key)
    {
        var data = await GetProfiles();
        if (data.Profiles.IndexIf(x => x.Key == key) == -1)
            return;
        data.DefaultProfileKey = key;
        await SaveProfiles(data);
    }

    public Profile GetCurrentProfile()
    {
        var profiles = GetProfilesSync();
        var name = profiles.Profiles.FirstOrDefault(p => p.Key == currentProfileKey)?.Name ?? currentProfileKey;
        return new Profile(currentProfileKey, name);
    }

    private struct CoreVersionSettings
    {
        public string? Version { get; set; }
    }
    
    private struct ThemesSettings
    {
        public double Hue { get; set; }
    }
}
