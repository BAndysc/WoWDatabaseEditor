using System;
using System.Threading.Tasks;
using WDE.Common.Managers;
using WDE.Common.Services.MessageBox;
using WDE.Module.Attributes;

namespace WDE.Common.Services;

public enum GuidType
{
    Creature,
    GameObject
}

[UniqueProvider]
public interface IPersonalGuidRangeService
{
    bool IsConfigured { get; }
    uint GetNextGuid(GuidType type);
    uint GetNextGuidRange(GuidType type, uint count);
}

public static class PersonalGuidRangeExtensions
{
    private static async Task<T?> Wrap<T>(Func<Task<T>> func, GuidType type, IStatusBar status)
    {
        try
        {
            return await func();
        }
        catch (GuidServiceNotSetupException)
        {
            return default;
        }
        catch (NoMoreGuidsException)
        {          
            status.PublishNotification(new PlainNotification(NotificationType.Warning, "No more guids available for " + type));
            return default;
        }
    }

    private static async Task<T?> Wrap<T>(Func<Task<T>> func, GuidType type, IMessageBoxService messageBoxService)
    {
        try
        {
            return await func();
        }
        catch (GuidServiceNotSetupException)
        {
            await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                .SetIcon(MessageBoxIcon.Error)
                .SetTitle("No configuration")
                .SetMainInstruction("Set up guid ranges first")
                .SetContent("You need to configure guid ranges in the settings.")
                .Build());
            return default;
        }
        catch (NoMoreGuidsException)
        {
            await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                .SetIcon(MessageBoxIcon.Error)
                .SetTitle("No more guids")
                .SetMainInstruction("No more guids")
                .SetContent("You are out of guids for " + type + ". Open settings and setup a new range to continue.")
                .Build());
            return default;
        }
    }
    
    public static async Task<uint?> GetNextGuidRangeOrShowError(this IPersonalGuidRangeService service, GuidType type, uint count, IMessageBoxService messageBoxService)
    {
        return await Wrap<uint?>(async () => service.GetNextGuidRange(type, count), type, messageBoxService);
    }
    
    public static async Task<uint?> GetNextGuidOrShowError(this IPersonalGuidRangeService service, GuidType type, IMessageBoxService messageBoxService)
    {
        return await Wrap<uint?>(async () => service.GetNextGuid(type), type, messageBoxService);
    }
    
    public static async Task<uint?> GetNextGuidRangeOrShowError(this IPersonalGuidRangeService service, GuidType type, uint count, IStatusBar statusBar)
    {
        return await Wrap<uint?>(async () => service.GetNextGuidRange(type, count), type, statusBar);
    }
    
    public static async Task<uint?> GetNextGuidOrShowError(this IPersonalGuidRangeService service, GuidType type, IStatusBar statusBar)
    {
        return await Wrap<uint?>(async () => service.GetNextGuid(type), type, statusBar);
    }
}

public class GuidServiceNotSetupException : Exception
{
    public GuidServiceNotSetupException() : base("GuidService is not configured")
    {
    }
}

public class NoMoreGuidsException : Exception
{
    public NoMoreGuidsException(GuidType type) : base("There is no more guid for type " + type)
    {
    }
}