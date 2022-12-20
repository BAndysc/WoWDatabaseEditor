using System.Windows.Input;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Services.MessageBox;
using WDE.Common.Utils;

namespace WDE.MapSpawnsEditor.Tools.ViewModels;

public partial class SpawnGroupPickerViewModel : IDialog
{
    private readonly IMessageBoxService messageBoxService;
    private readonly IDatabaseProvider databaseProvider;
    [Notify] private uint id;
    [Notify] private string name = "";
    [Notify] private GroupTypeViewModel groupType;
    [Notify] private bool isDialogEnabled = true;
    public List<GroupTypeViewModel> SpawnGroupTypes { get; }

    public bool HasSpawnGroupType { get; }
    
    public SpawnGroupPickerViewModel(ICurrentCoreVersion currentCoreVersion,
        IMessageBoxService messageBoxService,
        IDatabaseProvider databaseProvider)
    {
        this.messageBoxService = messageBoxService;
        this.databaseProvider = databaseProvider;
        Accept = new DelegateCommand(() => CheckAndClose().ListenErrors(),
            () => !string.IsNullOrEmpty(name) && name.Length < 200)
            .ObservesProperty(() => Name);
        Cancel = new DelegateCommand(() => CloseCancel?.Invoke());

        HasSpawnGroupType = currentCoreVersion.Current.DatabaseFeatures.SpawnGroupTemplateHasType;
        SpawnGroupTypes = new List<GroupTypeViewModel>()
        {
            new GroupTypeViewModel(SpawnGroupTemplateType.Creature, "Creature"),
            new GroupTypeViewModel(SpawnGroupTemplateType.GameObject, "GameObject"),
        };
        groupType = SpawnGroupTypes[0];
    }

    private async Task CheckAndClose()
    {
        IsDialogEnabled = false;
        try
        {
            var group = await databaseProvider.GetSpawnGroupTemplateByIdAsync(id);
            if (group == null)
                CloseOk?.Invoke();
            else
            {
                await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetMainInstruction("Spawn group already exists")
                    .SetContent($"This spawn group already exists ({group.Name}). Please choose another id.")
                    .WithOkButton(true)
                    .Build());
            }
        }
        finally
        {
            IsDialogEnabled = true;
        }
    }

    public int DesiredWidth => 400;
    public int DesiredHeight => 250;
    public string Title => "Spawn Group Picker";
    public bool Resizeable => true;
    public ICommand Accept { get; set; }
    public ICommand Cancel { get; set; }
    public event Action? CloseCancel;
    public event Action? CloseOk;

    public class GroupTypeViewModel
    {
        public SpawnGroupTemplateType Id { get; }
        public string Name { get; }

        public GroupTypeViewModel(SpawnGroupTemplateType id, string name)
        {
            Id = id;
            Name = name;
        }

        public override string ToString()
        {
            return $"{Name} ({(int)Id})";
        }
    }
}