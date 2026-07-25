using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common;
using WDE.Common.Managers;
using WDE.Common.Services.MessageBox;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MPQ.ViewModels;

namespace WDE.MapRenderer
{
    [AutoRegister(Platforms.Desktop)]
    public class WorldRendererSettingsViewModel : BindableBase, IFirstTimeWizardConfigurable
    {
        private readonly GameViewSettings gameViewSettings;
        private string? woWPath;
        private MpqOpenType mpqOpenType;
        private RenderPanelModeOption renderPanelMode;

        public WorldRendererSettingsViewModel(IMpqSettings mpqSettings,
            IWoWFilesVerifier verifier,
            IWindowManager windowManager,
            IMessageBoxService messageBoxService,
            GameViewSettings gameViewSettings)
        {
            this.gameViewSettings = gameViewSettings;
            woWPath = mpqSettings.Path;
            mpqOpenType = mpqSettings.OpenType;
            renderPanelMode = RenderPanelModes.First(x => x.UseCompositionPanel == gameViewSettings.UseCompositionPanel);

            Save = new DelegateCommand(() =>
            {
                mpqSettings.Path = woWPath;
                mpqSettings.OpenType = mpqOpenType;
                mpqSettings.Save();
                gameViewSettings.UseCompositionPanel = renderPanelMode.UseCompositionPanel;
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

        public IReadOnlyList<RenderPanelModeOption> RenderPanelModes { get; } = new List<RenderPanelModeOption>
        {
            new("Avalonia Synchronized", true),
            new("Multithreaded", false),
        };

        public RenderPanelModeOption RenderPanelMode
        {
            get => renderPanelMode;
            set
            {
                SetProperty(ref renderPanelMode, value);
                IsModified = true;
                RaisePropertyChanged(nameof(IsModified));
            }
        }

        public ICommand Save { get; set; }
        public string Name => "3D world renderer";
        public ImageUri Icon { get; } = new ImageUri("Icons/document_3d.png");
        public string ShortDescription =>
            "Client data files (3.3.5, 4.3.4, 5.4.8 and 7.3.5) and how the 3D world view is rendered.";
        public bool IsModified { get; set; }
        public bool IsRestartRequired => true;
        public ConfigurableGroup Group => ConfigurableGroup.Basic;
    }

    /// <summary>One choice in the 3D render-panel combo. <see cref="UseCompositionPanel"/> maps to
    /// <see cref="GameViewSettings.UseCompositionPanel"/>: true = ProperTheEnginePanel (renders through the
    /// Avalonia compositor, synchronized with the UI), false = NativeTheEnginePanel (native child window
    /// rendering on its own thread).</summary>
    public class RenderPanelModeOption
    {
        public string Name { get; }
        public bool UseCompositionPanel { get; }

        public RenderPanelModeOption(string name, bool useCompositionPanel)
        {
            Name = name;
            UseCompositionPanel = useCompositionPanel;
        }

        public override string ToString() => Name;
    }
}
