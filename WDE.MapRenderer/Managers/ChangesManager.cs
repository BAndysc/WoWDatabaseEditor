using WDE.Common.Disposables;
using WDE.MVVM.Observable;

namespace WDE.MapRenderer.Managers;

public interface ISavable
{
    Task Save();
    ReactiveProperty<bool> IsModified { get; }
}

public interface IChangesManager
{
    Task Save();
    ReactiveProperty<bool> IsModified { get; }
    System.IDisposable AddSavable(ISavable savable);
}

public class ChangesManager : IChangesManager
{
    private List<ISavable> savables = new();
    
    public async Task Save()
    {
        for (int i = savables.Count - 1; i >= 0; --i)
        {
            var savable = savables[i];
            await savable.Save();
        }
    }

    public ReactiveProperty<bool> IsModified { get; } = new ReactiveProperty<bool>(false);

    public System.IDisposable AddSavable(ISavable savable)
    {
        savables.Add(savable);
        savable.IsModified.SubscribeAction(_ => ReevaluateModified());
        return new ActionDisposable(() => savables.Remove(savable));
    }

    private void ReevaluateModified()
    {
        IsModified.Value = false;
        for (int i = savables.Count - 1; i >= 0; --i)
        {
            var savable = savables[i];
            if (savable.IsModified.Value)
                IsModified.Value = true;
        }
    }
}