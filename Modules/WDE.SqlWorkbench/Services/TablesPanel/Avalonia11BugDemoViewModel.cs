using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using DynamicData.Binding;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.Documents;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Services.Connection;
using WDE.SqlWorkbench.Services.SqlDump;
using WDE.SqlWorkbench.Services.TableUtils;
using WDE.SqlWorkbench.Services.UserQuestions;
using WDE.SqlWorkbench.Settings;
using WDE.SqlWorkbench.ViewModels;

namespace WDE.SqlWorkbench.Services.TablesPanel;


internal partial class Avalonia11BugDemoViewModel : ObservableBase, ITablesToolGroup
{
    [Notify] private bool isLoading;
    [Notify] private bool listUpdated;
    [Notify] private string searchText = "";
    [Notify] private INodeType? selected;

    private ObservableCollectionExtended<DemoSchemaViewModel> roots = new();
    public FlatTreeList<INamedParentType, INamedChildType> FlatItems { get; }
    
    public ImageUri Icon => ImageUri.Empty;
    public string GroupName => "Avalonia 11 Bug Demo";
    public RgbColor? CustomColor => null;
    public int Priority => -1;

    private CancellationTokenSource? loadCancellationTokenSource;

    public Avalonia11BugDemoViewModel()
    {
        FlatItems = new FlatTreeList<INamedParentType, INamedChildType>(roots);

        On(() => SearchText, DoFilter);
    }

    private string[] demoSchemas = new string[]
    {
        "vestibulum",
        "magna",
        "accumsan"
    };

    public void ToolOpened()
    {
        async Task LoadCoreAsync(CancellationToken cancellationToken)
        {
            try
            {
                IsLoading = true;
                roots.Clear();
                if (cancellationToken.IsCancellationRequested)
                    return;

                foreach (var schema in demoSchemas)
                    roots.Add(new DemoSchemaViewModel(this, schema));

                DoFilter(SearchText);
            }
            finally
            {
                IsLoading = false;
            }
        }

        loadCancellationTokenSource?.Cancel();
        loadCancellationTokenSource = new CancellationTokenSource();
        LoadCoreAsync(loadCancellationTokenSource.Token).ListenErrors();    
    }

    public void ToolClosed()
    {
        loadCancellationTokenSource?.Cancel();
        loadCancellationTokenSource = null;
    }

    private void DoFilter(string search)
    {
        bool UpdateVisibility(DemoDatabaseObjectGroupViewModel item)
        {
            bool anyChildVisible = false;
            foreach (var child in item.Children)
            {
                var childMatched = string.IsNullOrEmpty(search);
                if (child is INamedChildType namedChild)   
                    childMatched |= namedChild.Name.Contains(search, StringComparison.OrdinalIgnoreCase);
                child.IsVisible = childMatched;
                anyChildVisible |= childMatched;
            }
            
            return item.IsVisible = anyChildVisible;
        }
        
        bool UpdateVisibilityRecursive(DemoSchemaViewModel item)
        {
            bool anyChildVisible = false;
            var matched = string.IsNullOrEmpty(search) || item.SchemaName.Contains(search, StringComparison.OrdinalIgnoreCase);
            item.IsVisible = matched;
            
            foreach (var nestedParent in item.NestedParents.Cast<DemoDatabaseObjectGroupViewModel>())
                anyChildVisible |= UpdateVisibility(nestedParent);
            
            if (anyChildVisible)
                item.IsVisible = true;
            
            return anyChildVisible || matched;
        }
        
        foreach (var x in roots)
            UpdateVisibilityRecursive(x);

        ListUpdated = !ListUpdated;
    }

    public void Refilter() => DoFilter(SearchText);
}


internal partial class DemoDatabaseObjectGroupViewModel : ObservableBase, INamedParentType
{
    [Notify] private bool isExpanded;

    private ObservableCollection<IChildType> children = new ObservableCollection<IChildType>();

    public DemoDatabaseObjectGroupViewModel(DemoSchemaViewModel schema,
        string name,
        ImageUri icon,
        IReadOnlyList<IChildType> childList)
    {
        Parent = schema;
        Schema = schema;
        Name = name;
        Icon = icon;
        children.AddRange(childList);
        children.CollectionChanged += (sender, args) => ChildrenChanged?.Invoke(this, args);
    }

    public DemoSchemaViewModel Schema { get; }
    public uint NestLevel { get; set; }
    public bool IsVisible { get; set; }
    public IParentType? Parent { get; set; }
    public string Name { get; }
    public ImageUri Icon { get; }
    public bool CanBeExpanded => true;
    public IReadOnlyList<IParentType> NestedParents { get; } = Array.Empty<IParentType>();
    public IReadOnlyList<IChildType> Children => children;
    public event NotifyCollectionChangedEventHandler? ChildrenChanged;
    public event NotifyCollectionChangedEventHandler? NestedParentsChanged;
}

internal partial class DemoTableViewModel : ObservableBase, INamedChildType
{
    public DemoTableViewModel(DemoSchemaViewModel parent, string tableName, TableType type)
    {
        TableName = tableName;
        Type = type;
        Parent = parent;
        Schema = parent;
        if (type == TableType.Table)
            Icon = new ImageUri("Icons/icon_mini_table_big.png");
        else
            Icon = new ImageUri("Icons/icon_mini_view_big.png");
    }

    public string Name => TableName;
    public ImageUri Icon { get; }
    public string TableName { get; }
    public TableType Type { get; }
    public bool CanBeExpanded => false;
    public uint NestLevel { get; set; }
    public bool IsVisible { get; set; }
    public IParentType? Parent { get; set; }
    public DemoSchemaViewModel Schema { get; }
}

internal partial class DemoSchemaViewModel : ObservableBase, INamedParentType
{
    [Notify] private bool isExpanded = true;

    public ImageUri Icon => new ImageUri("Icons/icon_mini_schema_big.png");
    public Avalonia11BugDemoViewModel ParentViewModel { get; }
    public string SchemaName { get; }
    public string Name => SchemaName;

    private ObservableCollectionExtended<INamedChildType> children = new ObservableCollectionExtended<INamedChildType>();
    private ObservableCollectionExtended<DemoDatabaseObjectGroupViewModel> groups = new ObservableCollectionExtended<DemoDatabaseObjectGroupViewModel>();

    private CancellationTokenSource? loadToken;

    public DemoSchemaViewModel(Avalonia11BugDemoViewModel parentViewModel, string schemaName)
    {
        ParentViewModel = parentViewModel;
        SchemaName = schemaName;

        groups.CollectionChanged += (sender, args) => NestedParentsChanged?.Invoke(this, args);
        children.CollectionChanged += (sender, args) => ChildrenChanged?.Invoke(this, args);
        On(() => IsExpanded, @is =>
        {
            loadToken?.Cancel();
            if (@is)
            {
                loadToken = new CancellationTokenSource();
                LoadTablesAsync(loadToken.Token).ListenErrors();
            }
            else
                loadToken = null;
        });
    }

    private static string[] demoTables = new[]
    {
        "Lorem_ipsum_dolor_sit",
        "amet_consectetur_adipiscing_elit",
        "Integer_a_pretium_ipsum",
        "Quisque_volutpat_dui_non",
        "justo_dignissim_volutpat_eget",
        "vel_justo_Fusce_ante",
        "urna_fringilla_in_erat",
        "ut_lacinia_pretium_metus",
        "Nulla_facilisi_Aliquam_pellentesque",
        "eros_tortor_eu_fringilla",
        "ex_gravida_sit_amet",
        "Donec_erat_dolor_congue",
        "vel_accumsan_non_aliquam",
        "sit_amet_mauris_Fusce",
        "fringilla_sem_sit_amet",
        "enim_sollicitudin_ut_ultricies",
        "erat_pretium_Sed_varius",
        "nisi_vel_risus_accumsan",
        "at_egestas_purus_euismod",
        "Vestibulum_placerat_felis_ut",
        "gravida_consectetur_nunc_urna",
        "consequat_est_vitae_ultricies",
        "nisi_mi_vitae_neque",
        "Vestibulum_ornare_lacinia_nulla",
        "Vestibulum_faucibus_consequat_aliquam",
        "Duis_non_mauris_ante",
        "Pellentesque_dignissim_pretium_lacus",
        "vel_finibus_tellus_Morbi",
        "eu_lorem_nec_arcu",
        "vestibulum_iaculis_In_vel",
        "nibh_porta_dapibus_elit",
        "in_iaculis_nisl_Pellentesque",
        "sodales_leo_magna_ac",
        "lobortis_libero_gravida_eget",
        "Praesent_condimentum_lacinia_nibh",
        "sed_volutpat_mauris_facilisis",
        "nec_Aliquam_vel_dictum",
        "neque_Suspendisse_ornare_eros",
        "vel_nisi_dapibus_eleifend",
        "sodales_quis_metus_Maecenas",
        "at_elit_augue_Praesent",
        "posuere_odio_nec_ex",
        "lobortis_ut_maximus_felis",
        "interdum_Integer_diam_lorem",
        "condimentum_in_volutpat_ac",
        "luctus_sed_nisi_Vivamus",
        "quis_aliquam_velit_Praesent",
        "sagittis_orci_id_lectus",
        "imperdiet_rhoncus_Aliquam_elit",
        "mi_tincidunt_ut_sodales",
        "eu_efficitur_in_ipsum",
        "Nullam_in_pretium_leo",
        "Proin_suscipit_augue_diam",
        "nec_accumsan_ante_accumsan",
        "sed_Pellentesque_habitant_morbi",
        "tristique_senectus_et_netus",
        "et_malesuada_fames_ac",
        "turpis_egestas_Nullam_id",
        "odio_ligula_Vestibulum_iaculis",
        "nibh_vitae_tincidunt_lacinia",
        "Aenean_pulvinar_sapien_nisi",
        "vel_cursus_urna_tempus",
        "at_Proin_non_tortor",
        "vitae_dui_mattis_posuere",
        "Morbi_et_molestie_elit",
        "id_fringilla_ante_Pellentesque",
        "a_turpis_et_justo",
        "lacinia_finibus_Ut_blandit",
        "quis_neque_non_convallis",
        "Pellentesque_at_ipsum_quis",
        "orci_finibus_molestie_Phasellus",
        "posuere_metus_eu_ex",
        "suscipit_tristique_Praesent_sodales",
        "urna_massa_ut_feugiat",
        "sem_venenatis_eget_Nunc",
        "convallis_lacinia_laoreet_Vivamus",
        "quis_eleifend_felis_Donec",
        "vulputate_risus_id_vehicula",
        "dictum_quam_arcu_venenatis",
        "leo_vel_fermentum_magna",
        "risus_vel_orci_Vivamus",
        "id_mi_suscipit_purus",
        "accumsan_tempus_Ut_fermentum",
        "dictum_tellus_sed_molestie",
        "Ut_suscipit_faucibus_vehicula",
        "Cras_velit_mi_facilisis",
        "vel_bibendum_sed_tempor",
        "nec_massa_Integer_id",
        "egestas_justo_Vestibulum_gravida",
        "quam_in_purus_varius",
        "in_tincidunt_tortor_tristique",
        "Aliquam_erat_volutpat_Fusce",
        "dignissim_metus_vel_arcu",
        "scelerisque_eu_aliquam_enim",
        "scelerisque_Nullam_tempor_feugiat",
        "felis_vitae_lobortis_metus",
        "suscipit_id_Sed_dictum",
        "eget_lorem_eget_gravida",
        "Etiam_lobortis_lacus_eget",
        "justo_ullamcorper_eu_porttitor",
        "sapien_eleifend_In_laoreet",
        "diam_eget_urna_auctor",
        "imperdiet_Vivamus_rhoncus_sapien",
        "a_faucibus_luctus_Fusce",
        "in_mauris_rutrum_scelerisque",
        "odio_vitae_finibus_massa",
        "Proin_et_gravida_ante",
        "vel_auctor_augue_Curabitur",
        "magna_lectus_posuere_id",
        "lectus_vel_volutpat_maximus",
        "lectus_Pellentesque_dictum_rhoncus",
        "volutpat_Pellentesque_suscipit_convallis",
        "erat_vitae_lacinia_Praesent",
        "imperdiet_fringilla_arcu_eu",
        "placerat_metus_volutpat_id",
        "Sed_eu_mauris_imperdiet",
        "ipsum_ultrices_luctus_Nunc",
        "elementum_dolor_convallis_dolor",
        "semper_blandit_Suspendisse_tincidunt",
        "lacus_velit_vitae_semper",
        "lacus_efficitur_a_Nullam",
        "a_semper_urna_Mauris",
        "nec_risus_mattis_malesuada",
        "sem_sagittis_malesuada_turpis",
        "Ut_imperdiet_posuere_erat",
        "eget_ornare_felis_ornare",
        "eget_Etiam_accumsan_nulla",
        "non_sollicitudin_malesuada_turpis",
        "risus_fermentum_quam_et",
        "aliquam_dui_felis_quis",
        "enim_Nullam_posuere_ipsum",
        "id_tempus_placerat_Fusce",
        "mattis_rhoncus_felis_nec",
        "rhoncus_quam_interdum_laoreet",
        "Fusce_vitae_dui_euismod",
        "aliquet_orci_et_pellentesque",
        "velit_Mauris_in_ullamcorper",
        "libero_non_viverra_mi",
        "Maecenas_justo_mi_sodales",
        "sed_fermentum_a_pellentesque",
        "id_ipsum_Aliquam_erat",
        "volutpat_Nunc_tincidunt_tellus",
        "ut_lacus_iaculis_porta",
        "Ut_aliquam_elementum_metus",
        "eu_molestie_tellus_dapibus",
        "a_Maecenas_in_metus",
        "massa_Vestibulum_ornare_purus",
        "eget_pellentesque_commodo_orci",
        "velit_hendrerit_nulla_ut",
        "malesuada_mi_nibh_eget",
        "tellus_Pellentesque_habitant_morbi",
        "tristique_senectus_et_netus",
        "et_malesuada_fames_ac",
        "turpis_egestas_Nunc_gravida",
        "dui_nulla_dignissim_scelerisque",
        "nulla_egestas_non_Cras",
        "tincidunt_et_libero_a",
        "aliquam_Vivamus_feugiat_metus",
        "vitae_urna_mollis_eu",
        "aliquam_odio_eleifend_Vivamus",
        "feugiat_dolor_nibh_ac",
        "dignissim_orci_euismod_ut",
        "Aliquam_in_odio_odio",
        "Aliquam_eu_malesuada_ante",
        "Nulla_vel_accumsan_diam",
        "Nunc_vehicula_massa_id",
        "semper_mattis_Pellentesque_semper",
        "tristique_venenatis_Quisque_tristique",
        "metus_non_porta_laoreet",
        "quam_urna_dapibus_mi",
        "at_congue_odio_libero",
        "ut_ipsum_Nunc_ac",
        "mi_dolor_Proin_vel",
        "maximus_nunc_Nulla_vel",
        "enim_tortor_Phasellus_magna",
        "elit_semper_a_ipsum",
        "in_porttitor_maximus_arcu",
        "Integer_ac_finibus_dui",
        "Integer_nec_sollicitudin_urna",
        "sed_blandit_justo_Aliquam",
        "magna_leo_tincidunt_vel",
        "ultrices_ut_dignissim_ut",
        "sapien_Donec_mollis_placerat",
        "sollicitudin_Maecenas_vel_imperdiet",
        "mi_Nam_eget_neque",
        "ultricies_sagittis_diam_at",
        "venenatis_lorem_Phasellus_elementum",
        "justo_enim_non_ultrices",
        "quam_ullamcorper_id_Donec",
        "vitae_nisi_et_ligula",
        "scelerisque_mattis_Nulla_sollicitudin",
        "lobortis_dictum_Maecenas_accumsan",
        "nunc_a_dolor_tempus",
        "eleifend_Suspendisse_id_hendrerit",
        "est_ac_pulvinar_enim",
        "Curabitur_ultricies_erat_vehicula",
        "elit_ultricies_rhoncus_Nulla",
        "porttitor_condimentum_rhoncus_Cras",
        "metus_turpis_convallis_eu",
        "diam_ut_iaculis_semper",
        "elit_Proin_a_ex",
        "risus_Aenean_semper_est",
        "non_varius_pretium_Sed",
        "vel_porta_tortor_ut",
        "varius_justo_Sed_et",
        "odio_est_Praesent_dictum",
        "nunc_ac_nisi_rhoncus",
        "nec_euismod_lorem_ultricies",
        "Proin_id_libero_et",
        "neque_lacinia_iaculis_Vivamus",
        "quis_molestie_enim_Cras",
        "dictum_fermentum_magna_in",
        "viverra_eros_feugiat_eu",
        "Proin_enim_diam_placerat",
        "ut_varius_a_maximus",
        "et_felis_Praesent_ac",
        "congue_arcu_in_aliquet",
        "massa_Nunc_non_faucibus",
        "tellus_Morbi_feugiat_mattis",
        "est_nec_fermentum_turpis",
        "pharetra_vel_Phasellus_faucibus",
        "risus_purus_eget_iaculis",
        "nulla_ornare_sed_Sed",
        "vulputate_leo_mattis_aliquam",
        "porta_elit_dui_lacinia",
        "lorem_vel_mattis_ipsum",
        "felis_et_orci_Curabitur",
        "lobortis_augue_nisi_et",
        "ultricies_purus_interdum_in",
        "Suspendisse_sagittis_lectus_eros",
        "sed_posuere_nisi_pharetra",
        "at_In_eget_volutpat",
        "urna_a_molestie_urna",
        "Proin_in_quam_eros",
        "Phasellus_vestibulum_dui_eget",
        "magna_tempus_posuere_Vivamus",
        "in_quam_est_Sed",
        "vestibulum_consectetur_tellus_ac",
        "tempus_velit_tempor_id",
        "Aliquam_ante_sapien_laoreet",
        "sed_velit_at_feugiat",
        "imperdiet_sem_In_nunc",
        "sem_tempus_sit_amet",
        "leo_non_suscipit_hendrerit",
        "enim_Nam_facilisis_tellus",
        "in_vulputate_faucibus_leo",
        "dolor_varius_sapien_in",
        "faucibus_est_justo_vel",
        "leo_Vestibulum_rhoncus_dui",
        "ut_magna_semper_et"
    };

    private async Task LoadTablesAsync(CancellationToken token)
    {
        children.RemoveAll();
        groups.RemoveAll();

        var tables = demoTables
            .Select(x => new DemoTableViewModel(this, x, TableType.Table))
            .ToList();

        groups.RemoveAll();
        groups.Add(new DemoDatabaseObjectGroupViewModel(this, "Functions", new ImageUri("Icons/icon_mini_func.png"), Array.Empty<IChildType>()));
        groups.Add(new DemoDatabaseObjectGroupViewModel(this, "Procedures", new ImageUri("Icons/icon_mini_proc.png"), Array.Empty<IChildType>()));
        groups.Add(new DemoDatabaseObjectGroupViewModel(this, "Views", new ImageUri("Icons/icon_mini_view.png"), Array.Empty<IChildType>()));
        groups.Add(new DemoDatabaseObjectGroupViewModel(this, "Tables", new ImageUri("Icons/icon_mini_table.png"), tables));

        groups.Each(x => x.IsExpanded = true);

        ParentViewModel.Refilter();
    }

    public uint NestLevel { get; set; }
    public bool IsVisible { get; set; }
    public IParentType? Parent { get; set; }
    public IReadOnlyList<IParentType> NestedParents => groups;
    public IReadOnlyList<IChildType> Children => children;
    public bool CanBeExpanded => true;

    public event NotifyCollectionChangedEventHandler? ChildrenChanged;
    public event NotifyCollectionChangedEventHandler? NestedParentsChanged;
}