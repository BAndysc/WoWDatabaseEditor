using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Input;
using Avalonia.Threading;
using DynamicData.Binding;
using Newtonsoft.Json;
using PropertyChanged.SourceGenerator;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Services.MessageBox;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.DatabaseDefinitionEditor.Services;
using WDE.DatabaseDefinitionEditor.Settings;
using WDE.DatabaseDefinitionEditor.ViewModels.DefinitionEditor;
using WDE.DatabaseDefinitionEditor.ViewModels.DefinitionEditor.MetaColumns;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Services;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WDE.DatabaseDefinitionEditor.ViewModels;

[AutoRegister]
[SingleInstance]
public partial class DefinitionEditorViewModel : ObservableBase
{
    public readonly IDatabaseQueryExecutor QueryExecutor;
    public readonly IParameterFactory ParameterFactory;
    private readonly IDefinitionExporterService exporterService;
    private readonly IMessageBoxService messageBoxService;
    private readonly List<DefinitionStubViewModel> allDefinitions = new();

    public ObservableCollectionExtended<DefinitionStubViewModel> Definitions { get; } = new();
    public INativeTextDocument RawDefinitionDocument { get; }
    public IDefinitionGeneratorService GeneratorService { get; }
    public IMetaColumnViewModelFactory MetaColumnFactory { get; }
    public List<ICoreVersion> AllCoreVersions { get; }

    [Notify] private DataDatabaseType importTableDatabaseType = DataDatabaseType.World;

    [Notify] private string importTableName = "";
    
    [Notify] private string searchText = "";

    [Notify] private int selectedCellIndex;

    [Notify] private DefinitionSourceViewModel? selectedDefinitionSource;
    
    [Notify] private bool showIntroTip;

    public ICommand CreateEmptyTableCommand { get; }
    
    public ICommand ImportTableCommand { get; }
    
    public bool IsModified => selectedTable != null && IsTableModified(selectedTable);

    private DefinitionViewModel? selectedTable;
    public DefinitionViewModel? SelectedTable
    {
        get => selectedTable;
        set
        {
            if (selectedTable != null)
            {
                selectedTable.OnDataChanged -= Invalidate;
            }
            
            selectedTable = value;

            if (selectedTable != null)
            {
                selectedTable.OnDataChanged += Invalidate;
            }
            RaisePropertyChanged();
        }
    }

    private void Invalidate()
    {
        OnDataChanged?.Invoke();
    }

    public ObservableCollectionExtended<DemoItemGroup> ItemsDemo { get; } = new ObservableCollectionExtended<DemoItemGroup>();

    public event Action? OnDataChanged;
    
    private DefinitionStubViewModel? selectedDefinition;
    public DefinitionStubViewModel? SelectedDefinition
    {
        get => selectedDefinition;
        set
        {
            var oldStub = selectedDefinition;
            SetProperty(ref selectedDefinition, value);
            refreshInProgress?.Cancel();
            refreshInProgress = new CancellationTokenSource();
            messageBoxService.WrapError(() => SaveAndRefreshPreview(oldStub, value, refreshInProgress.Token)).ListenErrors();
        }
    }

    public ObservableCollectionExtended<ColumnValueTypeViewModel> AllParameters { get; } = new();
    
    public AsyncAutoCommand<ColumnViewModel> PickParameterCommand { get; }
    
    public DefinitionSourceViewModel JustAddedSource { get; } = new DefinitionSourceViewModel("Just added", new List<DefinitionStubViewModel>());
    
    public ObservableCollection<DefinitionSourceViewModel> DefinitionSources { get; } = new ObservableCollection<DefinitionSourceViewModel>();

    public DefinitionEditorViewModel(IEnumerable<ITableDefinitionEditorProvider> definitionProviders,
        IEnumerable<ICoreVersion> allCoreVersions,
        IDatabaseQueryExecutor queryExecutor,
        IParameterFactory parameterFactory,
        IParameterPickerService parameterPickerService,
        IDefinitionExporterService exporterService,
        IMessageBoxService messageBoxService,
        INativeTextDocument rawDefinitionDocument,
        IDefinitionEditorSettings settings,
        IDefinitionGeneratorService generatorService,
        IMetaColumnViewModelFactory metaColumnFactory,
        IWindowManager windowManager)
    {
        RawDefinitionDocument = rawDefinitionDocument;
        GeneratorService = generatorService;
        MetaColumnFactory = metaColumnFactory;
        AllCoreVersions = allCoreVersions.ToList();
        this.QueryExecutor = queryExecutor;
        this.ParameterFactory = parameterFactory;
        this.exporterService = exporterService;
        this.messageBoxService = messageBoxService;
        ShowIntroTip = !settings.IntroShown;
        settings.IntroShown = true;
        
        foreach (var provider in definitionProviders.Where(x => x.IsValid).OrderBy(x => x.Order))
            DefinitionSources.Add(new DefinitionSourceViewModel(provider.Name, provider.Definitions.Select(x => new DefinitionStubViewModel(x)).ToList()));
        
        if (DefinitionSources.Count > 0)
            SelectedDefinitionSource = DefinitionSources[0];

        PickParameterCommand = new AsyncAutoCommand<ColumnViewModel>(async vm =>
        {
            if (vm != null && vm.ValueType != null && vm.ValueType.IsParameter)
            {
                if (vm.ValueType.Parameter is IParameter<long> longParam)
                {
                    var (val, ok) = await parameterPickerService.PickParameter(longParam, 0);
                    if (ok)
                        vm.StringValue = val.ToString();
                }
                else if (vm.ValueType.Parameter is IParameter<string> stringParam)
                {
                    var (val, ok) = await parameterPickerService.PickParameter(stringParam, vm.StringValue ?? "");
                    if (ok)
                        vm.StringValue = val;
                }
            }
        });
        
        this.ToObservable<string, DefinitionEditorViewModel>(o => o.SearchText)
            .SubscribeAction(_ => DoSearch());
        
        this.ToObservable(o => o.SelectedCellIndex)
            .SubscribeAction(x =>
            {
                if (SelectedTable != null && x >= 0 && x < SelectedTable.ColumnsPreview.Count)
                    SelectedTable.SelectedColumnOrGroup = (ColumnViewModel)SelectedTable.ColumnsPreview[x];
            });
        
        On(() => SelectedDefinitionSource, def =>
        {
            allDefinitions.Clear();
            if (def != null)
                allDefinitions.AddRange(def.Definitions.OrderBy(x => x.TableName));
            SelectedDefinition = null;
            SelectedTable = null;
            DoSearch();
        });

        AllParameters.Add(ColumnValueTypeViewModel.FromValueType("long", true));
        AllParameters.Add(ColumnValueTypeViewModel.FromValueType("ulong", true));
        AllParameters.Add(ColumnValueTypeViewModel.FromValueType("int", true));
        AllParameters.Add(ColumnValueTypeViewModel.FromValueType("uint", true));
        AllParameters.Add(ColumnValueTypeViewModel.FromValueType("string", false));
        AllParameters.Add(ColumnValueTypeViewModel.FromValueType("float", true));
        foreach (var paramKey in parameterFactory.GetKeys())
        {
            AllParameters.Add(ColumnValueTypeViewModel.FromParameter(parameterFactory.Factory(paramKey), paramKey));
        }
        
        parameterFactory.OnRegisterKey().SubscribeAction(key =>
        {
            if (AllParameters.Any(x => x.ParameterName == key))
                return;
            AllParameters.Add(ColumnValueTypeViewModel.FromParameter(parameterFactory.Factory(key), key));
        });

        ImportTableCommand = new AsyncAutoCommand(async () =>
        {
            string? initialPath = null;
            if (selectedDefinition != null)
                initialPath = Path.GetDirectoryName(selectedDefinition.Definition.AbsoluteFileName);
            else if (allDefinitions.Count > 0)
                initialPath = Path.GetDirectoryName(allDefinitions[0].Definition.AbsoluteFileName);

            var resultFile = await windowManager.ShowSaveFileDialog("Json file|json", initialPath, importTableName + ".json");
            if (resultFile == null)
                return;
            
            if (!resultFile.EndsWith(".json"))
                resultFile += ".json";

            var definition = await generatorService.GenerateDefinition(new DatabaseTable(importTableDatabaseType, importTableName));
            
            definition.AbsoluteFileName = resultFile;
            definition.FileName = Path.GetRelativePath(Environment.CurrentDirectory, resultFile);
            
            File.WriteAllText(resultFile, Serialize(definition));

            var definitionVm = new DefinitionStubViewModel(definition);
            JustAddedSource.Definitions.Add(definitionVm);
            
            if (!DefinitionSources.Contains(JustAddedSource))
                DefinitionSources.Add(JustAddedSource);

            SelectedDefinitionSource = JustAddedSource;
            SelectedDefinition = definitionVm;
        });
        
        CreateEmptyTableCommand = new AsyncAutoCommand(async () =>
        {
            string? initialPath = null;
            if (selectedDefinition != null)
                initialPath = Path.GetDirectoryName(selectedDefinition.Definition.AbsoluteFileName);
            else if (allDefinitions.Count > 0)
                initialPath = Path.GetDirectoryName(allDefinitions[0].Definition.AbsoluteFileName);

            var resultFile = await windowManager.ShowSaveFileDialog("Json file|json", initialPath);
            if (resultFile == null)
                return;
            
            if (!resultFile.EndsWith(".json"))
                resultFile += ".json";

            var emptyDefinition = new DatabaseTableDefinitionJson();
            emptyDefinition.Id = emptyDefinition.TableName = Path.GetFileNameWithoutExtension(resultFile);
            emptyDefinition.AbsoluteFileName = resultFile;
            emptyDefinition.FileName = Path.GetRelativePath(Environment.CurrentDirectory, resultFile);
            
            File.WriteAllText(resultFile, Serialize(emptyDefinition));

            var definitionVm = new DefinitionStubViewModel(emptyDefinition);
            JustAddedSource.Definitions.Add(definitionVm);
            
            if (!DefinitionSources.Contains(JustAddedSource))
                DefinitionSources.Add(JustAddedSource);

            allDefinitions.Clear();
            allDefinitions.AddRange(JustAddedSource.Definitions.OrderBy(x => x.TableName));
            DoSearch();
            
            SelectedDefinitionSource = JustAddedSource;
            SelectedDefinition = definitionVm;
        });
    }

    private string Serialize(DatabaseTableDefinitionJson json)
    {
        return JsonConvert.SerializeObject(json, Formatting.Indented, new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore
        });
    }

    public void Save(DefinitionViewModel definition)
    {
        var exported = exporterService.Export(definition);
        File.WriteAllText(definition.SourceFile, Serialize(exported));
        definition.DefinitionStub.Definition = exported;
    }
    
    private bool IsTableModified(DefinitionViewModel definition)
    {
        var exported = exporterService.Export(definition);
        return !exported.Equals(definition.DefinitionStub.Definition);
    }

    private async Task<bool> EnsureSaved()
    {
        if (selectedTable == null)
            return true;
        
        if (!IsTableModified(selectedTable))
            return true;

        var result = await messageBoxService.ShowDialog(new MessageBoxFactory<int>()
            .SetTitle("Save changes")
            .SetMainInstruction("Do you want to save changes?")
            .SetContent("Unsaved changes will be lost.")
            .WithYesButton(0)
            .WithCancelButton(1)
            .WithNoButton(2)
            .Build());

        if (result == 1)
            return false;

        if (result == 0)
        {
            Save(selectedTable);
            return true;
        }

        return true;
    }
    
    private CancellationTokenSource? refreshInProgress;

    private async Task SaveAndRefreshPreview(DefinitionStubViewModel? old, DefinitionStubViewModel? definition,CancellationToken cancel)
    {
        if (!await EnsureSaved())
        {
            selectedDefinition = old;
            RaisePropertyChanged(nameof(SelectedDefinition));
            return;
        }

        await RefreshPreview(definition?.Definition, definition, cancel);
    }
    
    private async Task<bool> RefreshPreview(DatabaseTableDefinitionJson? json, DefinitionStubViewModel? definition, CancellationToken cancel)
    {
        if (definition == null || json == null)
        {
            SelectedTable = null;
            return false;
        }

        var tableVm = new DefinitionViewModel(this, json, definition);
        await tableVm.LoadDatabaseColumns();

        if (cancel.IsCancellationRequested)
            return false;
        
        foreach (var group in json.Groups)
        {
            var groupViewModel = new ColumnGroupViewModel(tableVm, group);
            foreach (var column in group.Fields)
            {
                var columnVm = tableVm.CreateColumn(groupViewModel, json, column);
                groupViewModel.Columns.Add(columnVm);
            }
            tableVm.AddGroup(groupViewModel);
        }

        tableVm.UpdateHasCustomPrimaryKey();

        ItemsDemo.RemoveAll();
        
        // sanity check - try to export the definition and see if it is the same.
        // it should be - otherwise we didn't load something and some data my be lost!
        var exported = exporterService.Export(tableVm);
        if (!exported.Equals(json))
        {
            for (int i = 0; i < exported.Groups.Count; ++i)
            {
                var tg = exported.Groups[i];
                var og = json.Groups[i];
                if (tg != og)
                {
                    for (int j = 0; j < tg.Fields.Count; ++j)
                    {
                        var tf = tg.Fields[j];
                        var of = og.Fields[j];
                        if (tf != of)
                        {Console.WriteLine("At index " + i + "/" + j + " there is a diff");}
                    }                    
                }
            }
            await messageBoxService.SimpleDialog("Error", "Can't open the definition","Apparently this definition has some properties that can't be edited by the definition editor. Please manually edit the json and/or report this to the author.");   
            return false;
        }
        else
        {
            File.WriteAllText(definition.Definition.AbsoluteFileName, Serialize(exported));
            ItemsDemo.Add(new DemoItemGroup(tableVm));
            SelectedTable = tableVm;
            UpdateRawDefinition();
            return true;
        }
    }
    
    public void UpdateRawDefinition()
    {
        if (selectedTable == null)
            return;
        var exported = exporterService.Export(selectedTable);
        RawDefinitionDocument.FromString(JsonConvert.SerializeObject(exported, Formatting.Indented, new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore
        }));
    }

    private void DoSearch()
    {
        using var _ = Definitions.SuspendNotifications();
        Definitions.Clear();
        if (string.IsNullOrWhiteSpace(searchText))
            Definitions.AddRange(allDefinitions);
        else
        {
            foreach (var def in allDefinitions)
            {
                if (def.TableName.Contains(searchText, StringComparison.InvariantCultureIgnoreCase))
                    Definitions.Add(def);
            }
        }
    }

    public async Task<bool> UpdateViewModelFromDefinition()
    {
        if (selectedDefinition == null)
            return false;
        
        try
        {
            var jsonString = RawDefinitionDocument.ToString();
            var def = JsonConvert.DeserializeObject<DatabaseTableDefinitionJson>(jsonString);
            if (def == null)
                throw new Exception("Can't parse JSON");
            
            def.AbsoluteFileName = selectedDefinition.Definition.AbsoluteFileName;
            def.FileName = selectedDefinition.Definition.FileName;
            refreshInProgress?.Cancel();
            refreshInProgress = new CancellationTokenSource();
            return await RefreshPreview(def, selectedDefinition, refreshInProgress.Token);
        }
        catch (JsonReaderException e)
        {
            await messageBoxService.SimpleDialog("Error", "Can't parse JSON", e.Message);
            return false;
        }
        catch (JsonSerializationException e)
        {
            await messageBoxService.SimpleDialog("Error", "Can't parse JSON", e.Message);
            return false;
        }
        catch (Exception e)
        {
            await messageBoxService.SimpleDialog("Error", "Can't parse JSON", e.ToString());
            return false;
        }
    }
}

public class DefinitionStubViewModel : ObservableBase
{
    private DatabaseTableDefinitionJson definition;

    public DefinitionStubViewModel(DatabaseTableDefinitionJson definition)
    {
        this.definition = definition;
        TableName = definition.TableName;
        RelativePath = definition.FileName;
    }

    public string TableName { get; }
    public string RelativePath { get; }

    public DatabaseTableDefinitionJson Definition
    {
        get => definition;
        set
        {
            var oldPath = definition.AbsoluteFileName;
            var oldRelativePath = definition.FileName;
            if (!string.IsNullOrEmpty(oldPath))
                value.AbsoluteFileName = oldPath;
            if (!string.IsNullOrEmpty(oldRelativePath))
                value.FileName = oldRelativePath;
            definition = value;
        }
    }
}

public class DefinitionSourceViewModel
{
    public DefinitionSourceViewModel(string sourceName, IReadOnlyList<DefinitionStubViewModel> definitions)
    {
        SourceName = sourceName;
        Definitions.AddRange(definitions);
    }

    public string SourceName { get; }

    public ObservableCollection<DefinitionStubViewModel> Definitions { get; } =new ObservableCollection<DefinitionStubViewModel>();
}