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
    private readonly ITabularDataPickerPreferences preferences;

    public TabularDataPicker(IWindowManager windowManager, ITabularDataPickerPreferences preferences)
    {
        this.windowManager = windowManager;
        this.preferences = preferences;
    }
    
    public async Task<T?> PickRow<T>(ITabularDataArgs<T> args, int defaultSelection, string? defaultSearchText = null) where T : class
    {
        List<int>? selection = null;
        if (defaultSelection >= 0)
            selection = new List<int>(1) {defaultSelection};
        
        using var viewModel = new TabularDataPickerViewModel(preferences, args.AsObject(), false, false, defaultSearchText ?? "", selection);
        
        var task = windowManager.ShowDialog(viewModel, out var window);
        if (window != null)
            preferences.SetupWindow(args.Title, window);
        
        if (!await task)
            return null;
        
        return viewModel.FocusedItem == null ? default : (T)viewModel.FocusedItem;
    }

    public async Task<IReadOnlyCollection<T>?> PickRows<T>(ITabularDataArgs<T> args, IReadOnlyList<int>? defaultSelection = null, string? defaultSearchText = null, bool useCheckBoxes = false) where T : class
    {
        using var viewModel = new TabularDataPickerViewModel(preferences, args.AsObject(), !useCheckBoxes, useCheckBoxes, defaultSearchText ?? "", defaultSelection);
        
        var task = windowManager.ShowDialog(viewModel, out var window);
        if (window != null)
            preferences.SetupWindow(args.Title, window);
        
        if (!await task)
            return null;
        
        return new CastCollection<T>(useCheckBoxes ? viewModel.CheckedItems : viewModel.SelectedItems);
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