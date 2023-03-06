using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WDE.Common.Utils;

namespace WDE.Common.Collections;

internal class SynchronizedIndexedCollectionElementWrapper<T> : INotifyPropertyChanged
{
    private bool isLoading = true;
    private T value = default!;

    public bool IsLoading => isLoading;
    public T Value => value;
    
    public SynchronizedIndexedCollectionElementWrapper(Task<T> getter)
    {
        UpdateValue(getter).ListenErrors();
    }

    private async Task UpdateValue(Task<T> getter)
    {
        value = await getter;
        isLoading = false;
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(Value));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}