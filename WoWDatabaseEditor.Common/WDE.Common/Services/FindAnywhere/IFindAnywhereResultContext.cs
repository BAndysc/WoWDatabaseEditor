using System.Collections.Generic;

namespace WDE.Common.Services.FindAnywhere;

public interface IFindAnywhereResultContext
{
    void AddResult(IFindAnywhereResult result);
}

public class MultiplexFindAnywhereResultContext : IFindAnywhereResultContext
{
    private readonly IFindAnywhereResultContext[] contexts;

    public MultiplexFindAnywhereResultContext(params IFindAnywhereResultContext[] contexts)
    {
        this.contexts = contexts;
    }
    
    public void AddResult(IFindAnywhereResult result)
    {
        foreach (var c in contexts)
            c.AddResult(result);
    }
}

public class ToListFindAnywhereResultContext : IFindAnywhereResultContext
{
    private readonly List<IFindAnywhereResult> results = new List<IFindAnywhereResult>();

    public IReadOnlyList<IFindAnywhereResult> Results => results;
    
    public void AddResult(IFindAnywhereResult result)
    {
        results.Add(result);
    }
}