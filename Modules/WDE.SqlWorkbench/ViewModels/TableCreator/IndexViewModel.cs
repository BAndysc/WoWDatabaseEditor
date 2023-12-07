using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PropertyChanged.SourceGenerator;
using WDE.MVVM;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Models.DataTypes;

namespace WDE.SqlWorkbench.ViewModels;

internal partial class IndexViewModel : ObservableBase
{
    [Notify] private string? indexName;
    [Notify] [AlsoNotify(nameof(CanPickType))] private IndexKind kind;
    [Notify] private IndexType type;
    [Notify] private string? comment;
    
    public ShowIndexEntry? OriginalIndexInfo { get; set; }
    
    public bool CanPickType => Kind is IndexKind.NonUnique or IndexKind.Unique;
    
    public ObservableCollection<IndexPartViewModel> Parts { get; } = new();
    
    public string ColumnsAsText => string.Join(", ", Parts.Select(p => p.ToString()));

    public bool IsNew => OriginalIndexInfo == null;
    
    public bool IsModified => OriginalIndexInfo is { } original &&
                              (original.KeyName != indexName ||
                               original.Kind != kind || 
                               original.Type != type ||
                               original.Comment != comment ||
                               Parts.Any(x => x.IsModified) ||
                               partsChanged);
    
    private bool partsChanged = false;
    
    public IndexViewModel()
    {
        Parts.CollectionChanged += (_, _) =>
        {
            partsChanged = true;
            RaisePropertyChanged(nameof(ColumnsAsText));
        };
        On(() => Kind, x =>
        {
            if (x is IndexKind.FullText or IndexKind.Spatial)
                Type = IndexType.Default;
        });
    }
    
    public IndexViewModel(IReadOnlyList<ColumnViewModel> columns, List<ShowIndexEntry> entries)
    {
        var original = entries[0];
        OriginalIndexInfo = original;
        IndexName = original.KeyName;
        Kind = original.Kind;
        Type = original.Type;
        Comment = original.IndexComment;
        foreach (var part in entries)
        {
            var partViewModel = new IndexPartViewModel(columns, part);
            Parts.Add(partViewModel);
        }
        
        Parts.CollectionChanged += (_, _) =>
        {
            partsChanged = true;
            RaisePropertyChanged(nameof(ColumnsAsText));
        };
        On(() => Kind, x =>
        {
            if (x is IndexKind.FullText or IndexKind.Spatial)
                Type = IndexType.Default;
        });
    }

    public IndexViewModel Clone()
    {
        var vm = new IndexViewModel()
        {
            IndexName = IndexName,
            Kind = Kind,
            Type = Type,
            Comment = Comment
        };
        foreach (var part in Parts)
            vm.Parts.Add(part.Clone());
        return vm;
    }
    
    public static List<IndexKind> Kinds { get; } = new() { IndexKind.NonUnique, IndexKind.Unique, IndexKind.FullText, IndexKind.Spatial };
    public static List<IndexType> Types { get; } = new() { IndexType.Default, IndexType.BTree, IndexType.Hash };
}