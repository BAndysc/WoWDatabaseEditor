using WDE.Common.Disposables;
using WDE.MVVM.Observable;

namespace WDE.MapRenderer.Managers;

public interface ISavable
{
    Task Save();
    ReactiveProperty<bool> IsModified { get; }

    /// <summary>The exact SQL <see cref="Save"/> would execute right now, or null when there is
    /// nothing pending. Pure - nothing executes, no state changes.</summary>
    Task<string?> GenerateQuery() => Task.FromResult<string?>(null);
}

public interface IChangesManager
{
    Task Save();
    /// <summary>The combined SQL a Save would execute right now (the document's "Generate query").</summary>
    Task<string> GenerateQuery();
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

    public async Task<string> GenerateQuery()
    {
        string? combined = null;
        for (int i = savables.Count - 1; i >= 0; --i)
        {
            var sql = await savables[i].GenerateQuery();
            if (!string.IsNullOrWhiteSpace(sql))
                combined = combined == null ? sql : combined + "\n" + sql;
        }
        return combined ?? "-- no pending 3D editor changes";
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