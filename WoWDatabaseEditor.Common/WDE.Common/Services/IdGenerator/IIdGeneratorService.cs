using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Managers;
using WDE.Common.Services.MessageBox;
using WDE.Module.Attributes;

namespace WDE.Common.Services.IdGenerator;

/// <summary>
/// The single entry point for allocating any kind of id in the editor. Collects all
/// registered <see cref="IIdSource"/>s; which source serves a given id kind is user
/// configurable when more than one is registered. Tracks ids handed out this session,
/// so database-derived strategies never return the same id twice for unsaved documents.
/// </summary>
[UniqueProvider]
public interface IIdGeneratorService
{
    /// <summary>Allocates the next free id.</summary>
    /// <exception cref="NoIdSourceException">no source registered for this id kind</exception>
    /// <exception cref="IdSourceNotConfiguredException">the active source needs setup in the settings</exception>
    /// <exception cref="NoMoreIdsException">the active source ran out of ids</exception>
    Task<long> GetNext(IIdType request);

    /// <summary>Allocates count consecutive ids, returns the first one. Same exceptions as GetNext.</summary>
    Task<long> GetNextRange(IIdType request, int count);

    /// <summary>
    /// Tells the generator about ids consumed outside of it, e.g. when a caller derives
    /// consecutive ids itself starting from a single GetNext result.
    /// </summary>
    void MarkUsed(IIdType request, long fromInclusive, long toInclusive);

    /// <summary>All id kinds any registered source can allocate (for the settings UI).</summary>
    IReadOnlyList<Type> KnownIdTypes { get; }

    IReadOnlyList<IIdSource> GetSources(Type idType);

    IIdSource? GetActiveSource(Type idType);

    void SetActiveSource(Type idType, IIdSource source);
}

public static class IdGeneratorExtensions
{
    public static async Task<long?> GetNextOrShowError(this IIdGeneratorService service, IIdType request, IMessageBoxService messageBoxService)
    {
        return await WrapDialog(() => service.GetNext(request), request, messageBoxService);
    }

    public static async Task<long?> GetNextRangeOrShowError(this IIdGeneratorService service, IIdType request, int count, IMessageBoxService messageBoxService)
    {
        return await WrapDialog(() => service.GetNextRange(request, count), request, messageBoxService);
    }

    public static async Task<long?> GetNextOrShowError(this IIdGeneratorService service, IIdType request, IStatusBar statusBar)
    {
        return await WrapStatus(() => service.GetNext(request), request, statusBar);
    }

    public static async Task<long?> GetNextRangeOrShowError(this IIdGeneratorService service, IIdType request, int count, IStatusBar statusBar)
    {
        return await WrapStatus(() => service.GetNextRange(request, count), request, statusBar);
    }

    private static async Task<long?> WrapDialog(Func<Task<long>> func, IIdType request, IMessageBoxService messageBoxService)
    {
        var idTypeName = IdTypeNames.Of(request.GetType());
        try
        {
            return await func();
        }
        catch (NoIdSourceException)
        {
            await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                .SetIcon(MessageBoxIcon.Error)
                .SetTitle("No id source")
                .SetMainInstruction("Can't generate an id")
                .SetContent($"No id source is able to generate '{idTypeName}' ids with the current core version.")
                .Build());
            return null;
        }
        catch (IdSourceNotConfiguredException e)
        {
            await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                .SetIcon(MessageBoxIcon.Error)
                .SetTitle("No configuration")
                .SetMainInstruction($"Set up '{e.SourceName}' first")
                .SetContent($"The source generating '{idTypeName}' ids needs to be configured in the settings (or pick a different source there).")
                .Build());
            return null;
        }
        catch (NoMoreIdsException)
        {
            await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                .SetIcon(MessageBoxIcon.Error)
                .SetTitle("No more ids")
                .SetMainInstruction("No more ids")
                .SetContent($"You are out of '{idTypeName}' ids. Open the settings to extend the range or pick a different source.")
                .Build());
            return null;
        }
    }

    private static async Task<long?> WrapStatus(Func<Task<long>> func, IIdType request, IStatusBar statusBar)
    {
        var idTypeName = IdTypeNames.Of(request.GetType());
        try
        {
            return await func();
        }
        catch (NoIdSourceException)
        {
            statusBar.PublishNotification(new PlainNotification(NotificationType.Warning, $"No id source can generate '{idTypeName}' ids"));
            return null;
        }
        catch (IdSourceNotConfiguredException e)
        {
            statusBar.PublishNotification(new PlainNotification(NotificationType.Warning, $"Configure '{e.SourceName}' in the settings to generate '{idTypeName}' ids"));
            return null;
        }
        catch (NoMoreIdsException)
        {
            statusBar.PublishNotification(new PlainNotification(NotificationType.Warning, $"No more '{idTypeName}' ids available"));
            return null;
        }
    }
}

public class NoIdSourceException : Exception
{
    public NoIdSourceException(Type idType) : base($"No id source registered for '{IdTypeNames.Of(idType)}' ids")
    {
    }
}

public class IdSourceNotConfiguredException : Exception
{
    public string SourceName { get; }

    public IdSourceNotConfiguredException(IIdSource source) : base($"Id source '{source.Name}' is not configured")
    {
        SourceName = source.Name;
    }
}

public class NoMoreIdsException : Exception
{
    public NoMoreIdsException(Type idType) : base($"There are no more '{IdTypeNames.Of(idType)}' ids available")
    {
    }
}
