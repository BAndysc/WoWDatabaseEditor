using System.Threading.Tasks;

namespace WDE.Common.Database;

public interface ICachedDatabaseProvider : IDatabaseProvider
{
    /// <summary>
    /// Returns cached creature template or null if not found in the cache. Can be used in places where one
    /// need a sync call, but the actual value is not crucial
    /// </summary>
    ICreatureTemplate? GetCachedCreatureTemplate(uint entry);
    
    /// <summary>
    /// Returns cached gameobject template or null if not found in the cache. Can be used in places where one
    /// need a sync call, but the actual value is not crucial
    /// </summary>
    IGameObjectTemplate? GetCachedGameObjectTemplate(uint entry);
    
    /// <summary>
    /// Returns cached quest template or null if not found in the cache. Can be used in places where one
    /// need a sync call, but the actual value is not crucial
    /// </summary>
    IQuestTemplate? GetCachedQuestTemplate(uint entry);

    Task WaitForCache();
}