using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Modules;
using WDE.Common.Services;
using WDE.Common.Services.IdGenerator;
using WDE.Common.Services.MessageBox;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.IdGeneratorService;

[AutoRegister]
[SingleInstance]
public class IdGeneratorService : IIdGeneratorService, IGlobalAsyncInitializer, System.IDisposable
{
    private readonly IUserSettings userSettings;
    private readonly Lazy<IMessageBoxService> messageBoxService;
    private readonly Dictionary<Type, List<IIdSource>> sourcesByType = new();
    private readonly Dictionary<Type, long> maxUsedByType = new();
    private readonly SemaphoreSlim mutex = new(1);
    private IdGeneratorSettings settings;

    public IdGeneratorService(IUserSettings userSettings,
        Lazy<IMessageBoxService> messageBoxService,
        IEnumerable<IIdSource> sources)
    {
        this.userSettings = userSettings;
        this.messageBoxService = messageBoxService;
        settings = userSettings.Get<IdGeneratorSettings>();
        foreach (var source in sources)
        {
            if (!sourcesByType.TryGetValue(source.IdType, out var list))
                sourcesByType[source.IdType] = list = new List<IIdSource>();
            list.Add(source);
        }
        foreach (var list in sourcesByType.Values)
            list.Sort((a, b) => b.DefaultPriority.CompareTo(a.DefaultPriority));
        KnownIdTypes = sourcesByType.Keys.OrderBy(IdTypeNames.Of).ToList();
    }

    /// <summary>Startup sanity check: every saved source choice must resolve to a registered
    /// source, otherwise (module unloaded, source renamed) the user is silently falling back
    /// to a default - warn them once.</summary>
    public async Task Initialize()
    {
        if (settings.ActiveSources == null || settings.ActiveSources.Count == 0)
            return;

        List<string> missing = new();
        foreach (var (typeName, sourceName) in settings.ActiveSources)
        {
            var idType = sourcesByType.Keys.FirstOrDefault(t => t.FullName == typeName);
            if (idType != null && sourcesByType[idType].Any(s => s.Name == sourceName))
                continue;
            var displayName = idType != null ? IdTypeNames.Of(idType) : typeName.Substring(typeName.LastIndexOf('.') + 1);
            missing.Add($"Couldn't find implementation for id provider for {displayName} (expected '{sourceName}').");
        }

        if (missing.Count > 0)
        {
            await messageBoxService.Value.ShowDialog(new MessageBoxFactory<bool>()
                .SetIcon(MessageBoxIcon.Warning)
                .SetTitle("Id generation")
                .SetMainInstruction("Missing id provider implementation")
                .SetContent(string.Join("\n", missing) +
                            "\n\nA default source will be used instead. You can pick a different one in the Id generation settings.")
                .Build());
        }
    }

    public async Task<long> GetNext(IIdType request) => await GetNextRange(request, 1);

    public async Task<long> GetNextRange(IIdType request, int count)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), count, "Can't allocate less than one id");

        var idType = request.GetType();
        var source = GetActiveSource(idType) ?? throw new NoIdSourceException(idType);
        if (!source.IsConfigured)
            throw new IdSourceNotConfiguredException(source);

        // serialized so that a source never sees a stale MaxUsedId
        await mutex.WaitAsync();
        try
        {
            var context = new IdGenerationContext
            {
                MaxUsedId = maxUsedByType.TryGetValue(idType, out var maxUsed) ? maxUsed : null
            };
            var first = await source.GetNext(request, count, context);
            MarkUsedNoLock(idType, first + count - 1);
            return first;
        }
        finally
        {
            mutex.Release();
        }
    }

    public void MarkUsed(IIdType request, long fromInclusive, long toInclusive)
    {
        MarkUsedNoLock(request.GetType(), toInclusive);
    }

    private void MarkUsedNoLock(Type idType, long upToInclusive)
    {
        if (!maxUsedByType.TryGetValue(idType, out var maxUsed) || maxUsed < upToInclusive)
            maxUsedByType[idType] = upToInclusive;
    }

    public IReadOnlyList<Type> KnownIdTypes { get; }

    public IReadOnlyList<IIdSource> GetSources(Type idType)
    {
        return sourcesByType.TryGetValue(idType, out var list) ? list : Array.Empty<IIdSource>();
    }

    public IIdSource? GetActiveSource(Type idType)
    {
        if (!sourcesByType.TryGetValue(idType, out var list))
            return null;
        if (settings.ActiveSources != null &&
            idType.FullName is { } key &&
            settings.ActiveSources.TryGetValue(key, out var sourceName) &&
            list.FirstOrDefault(s => s.Name == sourceName) is { } chosen)
            return chosen;
        // no saved choice: an unconfigured source (e.g. a personal range that was never set up)
        // must not win on priority alone, otherwise a fresh install can't generate any id
        return list.FirstOrDefault(s => s.IsConfigured) ?? list[0];
    }

    public void SetActiveSource(Type idType, IIdSource source)
    {
        if (source.IdType != idType)
            throw new ArgumentException($"Source '{source.Name}' doesn't generate '{IdTypeNames.Of(idType)}' ids");
        settings.ActiveSources ??= new Dictionary<string, string>();
        settings.ActiveSources[idType.FullName!] = source.Name;
        userSettings.Update(settings);
    }

    public void Dispose() => mutex.Dispose();
}

public struct IdGeneratorSettings : ISettings
{
    public Dictionary<string, string>? ActiveSources { get; set; }
}
