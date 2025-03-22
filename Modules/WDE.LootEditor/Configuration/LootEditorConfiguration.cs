using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using AvaloniaStyles.Controls.FastTableView;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Common.Utils.DragDrop;
using WDE.LootEditor.Editor;
using WDE.LootEditor.Services;
using WDE.Module.Attributes;
using WDE.MVVM;
using CollectionExtensions = System.Collections.ObjectModel.CollectionExtensions;

namespace WDE.LootEditor.Configuration;

[AutoRegister]
public partial class LootEditorConfiguration : ObservableBase, IConfigurable, IDropTarget
{
    public IWindowManager WindowManager { get; }
    public IFileSystem FileSystem { get; }
    public IParameterFactory ParameterFactory { get; }
    public ILootEditorFeatures EditorFeatures { get; }
    private readonly ILootEditorPreferences preferences;
    [Notify] private bool isModified;
    
    public ICommand Save { get; }
    public ImageUri Icon { get; } = new ImageUri("Icons/document_moneybagbitcoin_big.png");
    public string Name { get; } = "Loot editor";
    public string? ShortDescription => null;
    public bool IsRestartRequired => false;
    public ConfigurableGroup Group => ConfigurableGroup.Basic;

    public ObservableCollection<LootEditorButtonSettingViewModel> Buttons { get; } = new();

    [Notify] private ObservableCollection<LootEditorButtonSettingViewModel> selectedButtons = new();

    [Notify] private LootEditorButtonSettingViewModel? selectedButton;
    
    public DelegateCommand AddNewButtonCommand { get; }
    public DelegateCommand DeleteSelectedButtonsCommand { get; }

    public IReadOnlyList<ITableColumnHeader> CustomItemsColumns { get; } = new List<ITableColumnHeader>()
    {
        new TableTableColumnHeader("Item or currency", 60),
        new TableTableColumnHeader("Name", 150),
        new TableTableColumnHeader("Chance", 60),
        new TableTableColumnHeader("Loot mode", 90),
        new TableTableColumnHeader("Group id", 50),
        new TableTableColumnHeader("Min count or ref", 70),
        new TableTableColumnHeader("Max count", 60),
        new TableTableColumnHeader("Conditions", 100),
        new TableTableColumnHeader("Comment", 100),
    };

    public AsyncAutoCommand<CustomLootItemSettingViewModel> EditConditionsCommand { get; }

    internal LootEditorConfiguration(
        IWindowManager windowManager,
        IFileSystem fileSystem,
        IConditionEditService conditionEditService,
        IParameterFactory parameterFactory,
        ILootEditorFeatures editorFeatures,
        ILootEditorPreferences preferences)
    {
        WindowManager = windowManager;
        FileSystem = fileSystem;
        ParameterFactory = parameterFactory;
        EditorFeatures = editorFeatures;
        this.preferences = preferences;
        Save = new DelegateCommand(() =>
        {
            preferences.SaveButtons(Buttons.Select(x => x.ToDefinition()));
            IsModified = false;
        });

        EditConditionsCommand = new AsyncAutoCommand<CustomLootItemSettingViewModel>(async item =>
        {
            var key = new IDatabaseProvider.ConditionKey(editorFeatures.GetConditionSourceTypeFor(LootSourceType.Creature))
                .WithGroup(0)
                .WithEntry((int)item.ItemOrCurrencyId);
            var newConditions = await conditionEditService.EditConditions(key, item.Conditions);
            if (newConditions != null)
                item.Conditions = newConditions.ToList();
        });
        
        AddNewButtonCommand = new DelegateCommand(() =>
        {
            IsModified = true;
            Buttons.Add(new LootEditorButtonSettingViewModel(this, new LootButtonDefinition()
            {
                ButtonText = "New button"
            }));
        });

        DeleteSelectedButtonsCommand = new DelegateCommand(() =>
        {
            IsModified = true;
            Enumerable.ToList<LootEditorButtonSettingViewModel>(SelectedButtons).Each(x => Buttons.Remove(x));
        });

        Buttons.CollectionChanged += OnButtonsChanged;
        
        foreach (var def in preferences.Buttons.Value)
        {
            Buttons.Add(new LootEditorButtonSettingViewModel(this, def));
        }

        IsModified = false;
    }

    private void OnButtonsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (LootEditorButtonSettingViewModel old in e.OldItems)
            {
                old.PropertyChanged -= OnButtonPropertyChanged;
            }
        }
        if (e.NewItems != null)
        {
            foreach (LootEditorButtonSettingViewModel @new in e.NewItems)
            {
                @new.PropertyChanged += OnButtonPropertyChanged;
            }
        }
    }

    private void OnButtonPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        IsModified = true;
    }

    public void DragOver(IDropInfo dropInfo)
    {
        dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
        dropInfo.Effects = DragDropEffects.Move;
    }

    public void Drop(IDropInfo dropInfo)
    {
        IReadOnlyList<LootEditorButtonSettingViewModel> dragged;

        if (dropInfo.Data is IReadOnlyList<LootEditorButtonSettingViewModel> dragged2)
        {
            dragged = dragged2;
        }
        else if (dropInfo.Data is LootEditorButtonSettingViewModel drag)
        {
            dragged = new[] { drag };
        }
        else
            return;

        int dropIndex = dropInfo.InsertIndex;

        foreach (var x in dragged)
        {
            int indexOf = Buttons.IndexOf(x);
            if (indexOf < dropIndex)
                dropIndex--;
        }

        foreach (var x in dragged)
        {
            Buttons.Remove(x);
        }
        
        foreach (var x in dragged)
        {
            Buttons.Insert(dropIndex++, x);
            SelectedButton = x;
        }

        SelectedButtons.Clear();
        CollectionExtensions.AddRange(SelectedButtons, dragged);
        IsModified = true;
    }
}