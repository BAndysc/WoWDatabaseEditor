using Prism.Ioc;
using WDE.Module;
using WDE.QueryGenerators.Base;

namespace WDE.QueryGenerators;

public class QueryGeneratorModule : ModuleBase
{
    public override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        base.RegisterTypes(containerRegistry);
        containerRegistry.RegisterSingleton(typeof(IQueryGenerator<>), typeof(QueryGenerator<>));
    }
}