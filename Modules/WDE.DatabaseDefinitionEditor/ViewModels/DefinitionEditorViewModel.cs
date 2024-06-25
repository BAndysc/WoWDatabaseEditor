using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Input;
using Avalonia.Threading;
using DynamicData.Binding;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PropertyChanged.SourceGenerator;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Services;
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
    private readonly IVersionedFilesService versionedFilesService;
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

    private bool waitingForDefinitionChange;
    private DefinitionStubViewModel? selectedDefinition;
    public DefinitionStubViewModel? SelectedDefinition
    {
        get => selectedDefinition;
        set
        {
            if (waitingForDefinitionChange)
                return;
            
            var oldStub = selectedDefinition;
            SetProperty(ref selectedDefinition, value);
            refreshInProgress?.Cancel();
            refreshInProgress = new CancellationTokenSource();
            messageBoxService.WrapError(() => SaveAndRefreshPreview(oldStub, value, refreshInProgress.Token)).ListenErrors();
        }
    }
    
    private DefinitionSourceViewModel? selectedDefinitionSource;
    public DefinitionSourceViewModel? SelectedDefinitionSource
    {
        get => selectedDefinitionSource;
        set
        {
            if (waitingForDefinitionChange)
                return;

            var oldStub = selectedDefinition;
            var oldSource = selectedDefinitionSource;
            SetProperty(ref selectedDefinitionSource, value);
            
            refreshInProgress?.Cancel();
            refreshInProgress = new CancellationTokenSource();
            messageBoxService.WrapError(async () =>
            {
                if (!await SaveAndRefreshPreview(oldStub, null, refreshInProgress.Token))
                {
                    SetProperty(ref selectedDefinitionSource, oldSource);
                }
                else
                {
                    if (oldSource != null)
                        oldSource.Definitions.CollectionChanged -= OnSelectedSourceDefinitionsChanged;
                    allDefinitions.Clear();
                    if (value != null)
                    {
                        allDefinitions.AddRange(value.Definitions.OrderBy(x => x.TableName));
                        value.Definitions.CollectionChanged += OnSelectedSourceDefinitionsChanged;
                    }
                    SelectedDefinition = null;
                    SelectedTable = null;
                    DoSearch();   
                }
            }).ListenErrors();
        }
    }

    private void OnSelectedSourceDefinitionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (DefinitionStubViewModel item in e.NewItems!)
                {
                    allDefinitions.Add(item);
                    if (IsStubMatched(item))
                        Definitions.Add(item);
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                foreach (DefinitionStubViewModel item in e.OldItems!)
                {
                    allDefinitions.Remove(item);
                    Definitions.Remove(item);
                }
                break;
            case NotifyCollectionChangedAction.Replace:
                throw new ArgumentOutOfRangeException();
            case NotifyCollectionChangedAction.Move:
                throw new ArgumentOutOfRangeException();
            case NotifyCollectionChangedAction.Reset:
            {
                allDefinitions.Clear();
                foreach (var definition in SelectedDefinitionSource!.Definitions)
                    allDefinitions.Add(definition);
                DoSearch();
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
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
        IVersionedFilesService versionedFilesService,
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
        this.versionedFilesService = versionedFilesService;
        ShowIntroTip = !settings.IntroShown;
        settings.IntroShown = true;

        foreach (var provider in definitionProviders.Where(x => x.IsValid).OrderBy(x => x.Order))
            DefinitionSources.Add(AutoDispose(new DefinitionSourceViewModel(provider.Name, provider)));

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

            var handle = versionedFilesService.OpenFile(new FileInfo(resultFile));
            versionedFilesService.WriteAllText(handle, Serialize(definition));

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
            emptyDefinition.TableName = Path.GetFileNameWithoutExtension(resultFile);
            emptyDefinition.AbsoluteFileName = resultFile;
            emptyDefinition.FileName = Path.GetRelativePath(Environment.CurrentDirectory, resultFile);

            var handle = versionedFilesService.OpenFile(new FileInfo(resultFile));
            versionedFilesService.WriteAllText(handle, Serialize(emptyDefinition));

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
        versionedFilesService.WriteAllText(definition.SourceFile, Serialize(exported));
        versionedFilesService.MarkModified(definition.SourceFile);
        definition.DefinitionStub.Definition = exported;
    }
    
    private bool IsTableModified(DefinitionViewModel definition)
    {
        var exported = exporterService.Export(definition);
        return !exported.Equals(definition.DefinitionStub.Definition);
    }

    public async Task<bool> EnsureSaved()
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

    private async Task<bool> SaveAndRefreshPreview(DefinitionStubViewModel? old, DefinitionStubViewModel? definition,CancellationToken cancel)
    {
        waitingForDefinitionChange = true;
        try
        {
            if (!await EnsureSaved())
            {
                selectedDefinition = old;
                RaisePropertyChanged(nameof(SelectedDefinition));
                return false;
            }

            await RefreshPreview(definition?.Definition, definition, cancel);
        }
        finally
        {
            waitingForDefinitionChange = false;
        }

        return true;
    }
    
    private async Task<bool> RefreshPreview(DatabaseTableDefinitionJson? json, DefinitionStubViewModel? definition, CancellationToken cancel)
    {
        if (definition == null || json == null)
        {
            SelectedTable = null;
            return false;
        }

        var tableVm = new DefinitionViewModel(versionedFilesService, this, json, definition);
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
                        {
                            LOG.LogWarning(new StringBuilder().Append("At index ")
                                .Append(i)
                                .Append($" ({exported.Groups[i].Name})")
                                .Append("/")
                                .Append(j)
                                .Append($" ({tf.DbColumnName})")
                                .Append(" there is a diff (")
                                .Append(Path.GetRelativePath(Environment.CurrentDirectory, json.AbsoluteFileName))
                                .AppendLine(")")
                                .AppendLine("Original: " + JsonConvert.SerializeObject(of))
                                .Append("Exported: " + JsonConvert.SerializeObject(tf))
                                .ToString());
                        }
                    }                    
                }
            }
            await messageBoxService.SimpleDialog("Error", "Can't open the definition","Apparently this definition has some properties that can't be edited by the definition editor. Please manually edit the json and/or report this to the author.");   
            return false;
        }
        else
        {
            var fh = versionedFilesService.OpenFile(new FileInfo(definition.Definition.AbsoluteFileName));
            versionedFilesService.WriteAllText(fh, Serialize(exported));
            // versionedFilesService.StageFile(fh); <-- don't stage here, stage only if the user explicitly saves
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
            foreach (var def in allDefinitions.Where(IsStubMatched))
            {
                Definitions.Add(def);
            }
        }
    }

    private bool IsStubMatched(DefinitionStubViewModel def)
    {
        return string.IsNullOrWhiteSpace(searchText) || def.TableName.Contains(searchText, StringComparison.InvariantCultureIgnoreCase);
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

public partial class DefinitionStubViewModel : ObservableBase
{
    private DatabaseTableDefinitionJson definition;

    public DefinitionStubViewModel(DatabaseTableDefinitionJson definition)
    {
        this.definition = definition;
        RelativePath = definition.FileName;
        tableName = definition.TableName;
        icon = definition.IconPath != null ? new ImageUri(definition.IconPath) : ImageUri.Empty;
    }

    [Notify] private ImageUri? icon;
    [Notify] private string tableName;
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
            TableName = value.TableName;
            Icon = value.IconPath != null ? new ImageUri(value.IconPath) : ImageUri.Empty;
        }
    }
}

public class DefinitionSourceViewModel : IDisposable
{
    private readonly ITableDefinitionEditorProvider? definitionsProvider;

    public string SourceName { get; }

    public ObservableCollectionExtended<DefinitionStubViewModel> Definitions { get; } = new ObservableCollectionExtended<DefinitionStubViewModel>();

    private Dictionary<string, DefinitionStubViewModel> definitionsByRelativePath = new();

    public DefinitionSourceViewModel(string sourceName, IReadOnlyList<DefinitionStubViewModel> definitions)
    {
        SourceName = sourceName;
        Definitions.AddRange(definitions);
    }

    public DefinitionSourceViewModel(string sourceName, ITableDefinitionEditorProvider definitionsProvider)
    {
        this.definitionsProvider = definitionsProvider;
        SourceName = sourceName;
        definitionsProvider.DefinitionsChanged += OnDefinitionsChanged;
        OnDefinitionsChanged();
    }

    public void Dispose()
    {
        if (definitionsProvider != null)
            definitionsProvider.DefinitionsChanged -= OnDefinitionsChanged;
    }

    private void OnDefinitionsChanged()
    {
        if (definitionsProvider == null)
            return;

        var set = new HashSet<string>();

        foreach (var definition in definitionsProvider.Definitions)
        {
            var stub = new DefinitionStubViewModel(definition);
            set.Add(stub.RelativePath);
            if (definitionsByRelativePath.TryGetValue(stub.RelativePath, out var existingVm))
                existingVm.Definition = definition;
            else
            {
                Definitions.Add(stub);
                definitionsByRelativePath[stub.RelativePath] = stub;
            }
        }

        for (var index = Definitions.Count - 1; index >= 0; index--)
        {
            var definition = Definitions[index];
            if (!set.Contains(definition.RelativePath))
            {
                Definitions.RemoveAt(index);
                definitionsByRelativePath.Remove(definition.RelativePath);
            }
        }
    }
}