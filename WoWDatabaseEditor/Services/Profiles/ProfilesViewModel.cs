using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.CoreVersion;
using WDE.Common.Managers;
using WDE.Common.Profiles;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MVVM;

namespace WoWDatabaseEditorCore.Services.Profiles;

[AutoRegister]
[SingleInstance]
public partial class ProfilesViewModel : ObservableBase
{
    [Notify] private bool refreshing;
    private readonly IProfileService profileService;

    public ObservableCollection<ProfileViewModel> Profiles { get; } = new ObservableCollection<ProfileViewModel>();
    public ObservableCollection<ProfileViewModel> RunningProfiles { get; } = new ObservableCollection<ProfileViewModel>();
    
    public ICommand CreateNewProfileCommand { get; }
    public AsyncAutoCommand<ProfileViewModel> StartNewEditor { get; }
    public DelegateCommand<ProfileViewModel> SwitchToEditor { get; }
    public AsyncAutoCommand<ProfileViewModel> DeleteProfile { get; }
    public AsyncAutoCommand<ProfileViewModel> EditProfile { get; }
    
    public ProfilesViewModel(IProfileService profileService,
        IWindowManager windowManager,
        IMessageBoxService messageBoxService,
        ICurrentCoreVersion currentCoreVersion,
        Func<ProfileCreateViewModel> createViewModel)
    {
        this.profileService = profileService;

        CurrentProfileIcon = currentCoreVersion.Current.Icon;
        CurrentProfileName = profileService.GetCurrentProfile().Name;

        StartNewEditor = new AsyncAutoCommand<ProfileViewModel>(async profile =>
        {
            profileService.StartProfile(profile.Key);
            await Task.Delay(6000); // let's wait some time before reenabling this command, the editor is starting
        });

        SwitchToEditor = new DelegateCommand<ProfileViewModel>(p =>
        {
            if (p.Profile != null)
                profileService.SwitchToInstance(p.Profile.Value);
        });

        DeleteProfile = new AsyncAutoCommand<ProfileViewModel>(async p =>
        {
            if (!await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetTitle("Delete profile")
                    .SetMainInstruction($"Do you want to delete {p.ProfileName}?")
                    .SetContent(
                        $"This will delete the profile with all its settings.\n\nThis action can't be undone.")
                    .WithYesButton(true)
                    .WithNoButton(false)
                    .Build()))
                return;

            await profileService.DeleteProfile(p.Key);
        });
        
        CreateNewProfileCommand = new DelegateCommand(() =>
        {
            using var vm = createViewModel();
            windowManager.ShowDialog(vm).ListenErrors();
        });

        EditProfile = new AsyncAutoCommand<ProfileViewModel>(async p =>
        {
            using var vm = createViewModel();
            vm.ProfileName = p.ProfileName;
            vm.NoCreate = true;
            if (await windowManager.ShowDialog(vm))
            {
                if (vm.MakeDefault)
                    await profileService.SetDefaultProfile(p.Key);
                if (vm.ProfileName != p.ProfileName)
                    await profileService.RenameProfile(p.Key, vm.ProfileName);
            }
        });
    }

    private bool isOpen;
    public bool IsOpen
    {
        get => isOpen;
        set
        {
            if (value)
            {
                if (!refreshing)
                    Refresh().ListenErrors();
            }
            isOpen = value;
        }
    }

    public ImageUri CurrentProfileIcon { get; }
    
    public string CurrentProfileName { get; }

    public async Task Refresh()
    {
        Refreshing = true;
        try
        {
            var profiles = await profileService.GetProfiles();
            var runningProfiles = await profileService.GetRunningProfiles();
            
            Profiles.Clear();
            RunningProfiles.Clear();
            Dictionary<string, string> keyToName = new();
            foreach (var profile in profiles.Profiles)
            {
                var coreVersion = profileService.GetCoreVersionForProfile(profile.Key);
                Profiles.Add(
                    ProfileViewModel.FromProfile(profile,
                        coreVersion?.Icon,
                        StartNewEditor,
                        DeleteProfile,
                        EditProfile));
                keyToName[profile.Key] = profile.Name;
            }
            
            foreach (var profile in runningProfiles)
            {
                var coreVersion = profileService.GetCoreVersionForProfile(profile.Key);
                
                if (!keyToName.TryGetValue(profile.Key, out var name))
                    name = "(unknown instance)";

                RunningProfiles.Add(
                    ProfileViewModel.FromRunningProfile(profile,
                        name,
                        coreVersion?.Icon,
                        SwitchToEditor));
            }
        }
        finally
        { 
            Refreshing = false;
        }
    }
}

public class ProfileViewModel
{
    public string Header { get; init; } = null!;
    public ImageUri Icon { get; init;}
    public ICommand Command { get; init;} = null!;
    public string Key { get; init;} = null!;
    public bool CanDelete => !Profile.HasValue;
    public ICommand DeleteCommand { get; init; } = null!;
    public string ProfileName { get; init;} = null!;
    public bool CanEdit => !Profile.HasValue;
    public ICommand EditCommand { get; init;} = null!;
    public RunningProfile? Profile { get; init; }

    public static ProfileViewModel FromProfile(Profile profile, ImageUri? icon, ICommand runCommand,
        ICommand deleteCommand, ICommand editCommand)
    {
        return new ProfileViewModel()
        {
            Header = "Start new " + profile.Name,
            Icon = icon  ?? new ImageUri("Icons/core_unknown.png"),
            Key = profile.Key,
            ProfileName = profile.Name,
            Command = runCommand,
            DeleteCommand = deleteCommand,
            EditCommand = editCommand
        };
    }
    
    public static ProfileViewModel FromRunningProfile(RunningProfile profile, string name, ImageUri? icon, ICommand switchToCommand)
    {
        return new ProfileViewModel()
        {
            Header = "Switch to " + name,
            Icon = icon  ?? new ImageUri("Icons/core_unknown.png"),
            Key = profile.Key,
            ProfileName = name,
            Profile = profile,
            Command = switchToCommand
        };
    }
}