using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Prism.Ioc;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.Module;
using WDE.MVVM.Observable;
using WDE.WorldStateExpressions.Parameters;

[assembly: InternalsVisibleTo("WDE.WorldStateExpressions.Avalonia")]
[assembly: InternalsVisibleTo("WDE.WorldStateExpressions.Test")]
namespace WDE.WorldStateExpressions
{
    public class WorldStateExpressionsModule : ModuleBase
    {
        public override void OnInitialized(IContainerProvider containerProvider)
        {
            var worldStateNames = new WorldStateNameParameter();
            var loadingEventAggregator = containerProvider.Resolve<ILoadingEventAggregator>();

            loadingEventAggregator.OnEvent<EditorLoaded>().SubscribeOnce(_ =>
            {
                var factory = containerProvider.Resolve<IParameterFactory>();
                factory.Register("WorldStateNameParameter", worldStateNames);
                var expressionParameter = containerProvider.Resolve<WorldStateExpressionParameter>();
                expressionParameter.WorldStateNames = worldStateNames;
                factory.Register("WorldStateExpressionStringParameter", expressionParameter);
            });

            loadingEventAggregator.OnEvent<DatabaseLoadedEvent>().SubscribeAction(_ =>
            {
                async Task LoadNames()
                {
                    await worldStateNames.Load(containerProvider.Resolve<IMySqlExecutor>());
                    containerProvider.Resolve<IParameterFactory>().Updated(worldStateNames);
                }
                LoadNames().ListenErrors();
            });
        }
    }
}
