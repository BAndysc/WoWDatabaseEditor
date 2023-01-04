using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace WoWDatabaseEditorCore.Avalonia.Controls;

public class PseudoWindowsPanel : Panel
{
    private Dictionary<FakeWindowControl, TaskCompletionSource<bool>> windows = new();

    public async Task<bool> ShowDialog(FakeWindow ob)
    {
        FakeWindowControl fakeWindowControl = new();
        fakeWindowControl.Content = ob;
        fakeWindowControl.DataContext = ob.DataContext;
        Children.Add(fakeWindowControl);
        var tcs = new TaskCompletionSource<bool>();
        windows.Add(fakeWindowControl, tcs);
        return await tcs.Task;
    }

    public async Task Show(FakeWindow ob)
    {
        FakeWindowControl fakeWindowControl = new();
        fakeWindowControl.Content = ob;
        fakeWindowControl.DataContext = ob.DataContext;
        Children.Add(fakeWindowControl);
        var tcs = new TaskCompletionSource<bool>();
        windows.Add(fakeWindowControl, tcs);
        await tcs.Task;
    }

    public void CloseWindow(FakeWindowControl fakeWindow, bool dialogResult)
    {
        if (windows.TryGetValue(fakeWindow, out var tcs))
        {
            tcs.SetResult(dialogResult);
            windows.Remove(fakeWindow);
            Children.Remove(fakeWindow);
        }
    }
}