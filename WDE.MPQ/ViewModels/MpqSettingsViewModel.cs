using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common;
using WDE.Common.Managers;
using WDE.Common.Services.MessageBox;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WDE.MPQ.ViewModels
{
    [AutoRegister(Platforms.Desktop)]
    public class MpqSettingsViewModel : BindableBase, IFirstTimeWizardConfigurable
    {
        private string? woWPath;
        private MpqOpenType mpqOpenType;

        public MpqSettingsViewModel(IMpqSettings mpqSettings, 
            IWoWFilesVerifier verifier, 
            IWindowManager windowManager,
            IMessageBoxService messageBoxService)
        {
            woWPath = mpqSettings.Path;
            mpqOpenType = mpqSettings.OpenType;
            
            Save = new DelegateCommand(() =>
            {
                mpqSettings.Path = woWPath;
                mpqSettings.OpenType = mpqOpenType;
                mpqSettings.Save();
                IsModified = false;
                RaisePropertyChanged(nameof(IsModified));
            });

            PickFolder = new AsyncAutoCommand(async () =>
            {
                var folder = await windowManager.ShowFolderPickerDialog(woWPath ?? "");
                if (folder != null)
                {
                    if (verifier.VerifyFolder(folder) == WoWFilesType.Invalid)
                    {
                        await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                            .SetTitle("WoW Client Data")
                            .SetMainInstruction("Invalid WoW folder")
                            .SetContent(
                                "This doesn't look like a correct WoW folder.\n\nSelect main game folder (wow.exe file must be there).\n\nOther WoW versions are not supported now.")
                            .WithOkButton(true)
                            .Build());
                    }
                    else
                        WoWPath = folder;
                }
            });
        }
        
        public ICommand PickFolder { get; }

        public string? WoWPath
        {
            get => woWPath;
            set
            {
                SetProperty(ref woWPath, value);
                IsModified = true;
                RaisePropertyChanged(nameof(IsModified));
            }
        }
        
        public List<MpqOpenType> MpqOpenTypes { get; } = Enum.GetValues(typeof(MpqOpenType)).Cast<MpqOpenType>().ToList();
        
        public MpqOpenType MpqOpenType
        {
            get => mpqOpenType;
            set
            {
                SetProperty(ref mpqOpenType, value);
                IsModified = true;
                RaisePropertyChanged(nameof(IsModified));
            }
        }

        public ICommand Save { get; set; }
        public string Name => "Client data files";
        public string ShortDescription =>
            "The editor can open 3.3.5, 4.3.4, 5.4.8 and 7.3.5 files for extended features.";
        public bool IsModified { get; set; }
        public bool IsRestartRequired => true;
        public ConfigurableGroup Group => ConfigurableGroup.Basic;
    }
}