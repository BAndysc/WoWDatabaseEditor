using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Managers;
using WDE.Common.TableData;
using WDE.Module.Attributes;
using WoWDatabaseEditorCore.Avalonia.Services.TabularDataPickerService;

namespace WoWDatabaseEditorCore.Services.TabularDataPickerService;

[AutoRegister]
public class TabularDataPicker : ITabularDataPicker
{
    private readonly IWindowManager windowManager;

    public TabularDataPicker(IWindowManager windowManager)
    {
        this.windowManager = windowManager;
    }
    
    public async Task<T?> PickRow<T>(ITabularDataArgs<T> args, int defaultSelection, string? defaultSearchText = null) where T : class
    {
        List<int>? selection = null;
        if (defaultSelection >= 0)
            selection = new List<int>(1) {defaultSelection};
        
        using var viewModel = new TabularDataPickerViewModel(args.AsObject(), false, selection);
        if (defaultSearchText != null)
            viewModel.SearchText = defaultSearchText;
        await windowManager.ShowDialog(viewModel);
        return viewModel.FocusedItem == null ? default : (T)viewModel.FocusedItem;
    }

    public async Task<IReadOnlyCollection<T>> PickRows<T>(ITabularDataArgs<T> args, IReadOnlyList<int>? defaultSelection = null, string? defaultSearchText = null) where T : class
    {
        using var viewModel = new TabularDataPickerViewModel(args.AsObject(), true, defaultSelection);
        if (defaultSearchText != null)
            viewModel.SearchText = defaultSearchText;
        await windowManager.ShowDialog(viewModel);
        return new CastCollection<T>(viewModel.SelectedItems);
    }

    private class CastCollection<T> : IReadOnlyCollection<T>
    {
        private IReadOnlyCollection<object> untyped;

        public CastCollection(IReadOnlyCollection<object> untyped) 
            => this.untyped = untyped;

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var item in untyped)
                yield return (T)item;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Count => untyped.Count;
    }
}