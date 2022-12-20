using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common;
using WDE.Common.Managers;
using WDE.Common.Services.MessageBox;
using WDE.Common.Utils;
using WDE.FirstTimeWizard.Services;
using WDE.MVVM;
using WDE.Updater.Services;

namespace WDE.FirstTimeWizard.ViewModels;

public partial class FirstTimeWizardViewModel : ObservableBase, IDialog
{
    private readonly IUpdateService updateService;

    public FirstTimeWizardViewModel(IEnumerable<IFirstTimeWizardConfigurable> configs,
        ICoreVersionConfigurable coreVersionConfig,
        IMessageBoxService messageBoxService,
        IFirstTimeWizardSettings settings,
        IUpdateService updateService)
    {
        this.updateService = updateService;
        CoreVersionViewModel = coreVersionConfig;
        Configurables = configs.ToList();
        selectedConfigurable = Configurables[0];
        HasCoreVersion = settings.State == FirstTimeWizardState.HasCoreVersion;
        Accept = new AsyncAutoCommand(async () =>
        {
            if (NoCoreVersion)
            {
                settings.State = FirstTimeWizardState.HasCoreVersion;
                coreVersionConfig.Save.Execute(null);
            }
            else
            {
                foreach (var config in Configurables)
                {
                    config.Save.Execute(null);
                }

                settings.State = FirstTimeWizardState.Completed;
            }

            await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                .SetTitle("Restart required")
                .SetMainInstruction("Restart is required")
                .SetContent(
                    "In order to save the settings, you have to restart the editor.")
                .WithOkButton(true)
                .Build());

            await updateService.CloseForUpdate();
            
            CloseOk?.Invoke();
        });
        Cancel = new AsyncAutoCommand(async () =>
        {
            settings.State = FirstTimeWizardState.Canceled;
            
            await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                .SetTitle("First time wizard")
                .SetMainInstruction("Setup canceled")
                .SetContent(
                    "You have canceled the first time wizard, but don't worry, you can change the settings later in the File -> Settings menu.")
                .WithOkButton(true)
                .Build());
            
            CloseCancel?.Invoke();
        });
    }

    public object CoreVersionViewModel { get; }
    public List<IFirstTimeWizardConfigurable> Configurables { get; }
    [Notify] private IFirstTimeWizardConfigurable? selectedConfigurable;
    
    public bool NoCoreVersion => !HasCoreVersion;
    public bool HasCoreVersion { get; }
    
    public int DesiredWidth => 800;
    public int DesiredHeight => 600;
    public string Title => "First time wizard";
    public bool Resizeable => true;
    public ICommand Accept { get; }
    public ICommand Cancel { get; }

    public event Action? CloseCancel;
    public event Action? CloseOk;
}