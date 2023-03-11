using System.Threading.Tasks;
using Prism.Events;
using Prism.Ioc;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Events;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.Module;
using WDE.MVVM.Observable;
using WDE.Spells.Parameters;

namespace WDE.Spells
{
    public class SpellsModule : ModuleBase
    {
        private IContainerProvider containerProvider = null!;
        
        public override void OnInitialized(IContainerProvider containerProvider)
        {
            this.containerProvider = containerProvider;
            containerProvider.Resolve<IEventAggregator>()
                .GetEvent<AllModulesLoaded>()
                .Subscribe(() =>
                    {
                        var factory = containerProvider.Resolve<IParameterFactory>();
                        var spellPicker = this.containerProvider.Resolve<ISpellEntryProviderService>();
                        factory.RegisterCombined("SpellParameter", "DbcSpellParameter", "DatabaseSpellParameter", (dbc, db) => new SpellParameter(spellPicker, dbc, db), QuickAccessMode.Limited);
                        factory.RegisterDepending("MultiSpellParameter", "SpellParameter",  spells => new MultiSpellParameter(spells));
                        factory.RegisterDepending("SpellAreaSpellParameter", "SpellParameter", spells => new SpellAreaSpellParameter(spells));
                        factory.RegisterDepending("SpellOrRankedSpellParameter", "SpellParameter", spells => new SpellOrRankedSpellParameter(spells));
                    },
                    ThreadOption.PublisherThread,
                    true);

            containerProvider.Resolve<ILoadingEventAggregator>()
                .OnEvent<DatabaseLoadedEvent>()
                .SubscribeAction(_ =>
                {
                    LoadDatabaseSpellsAsync().ListenErrors();
                });
        }

        private async Task LoadDatabaseSpellsAsync()
        {
            var factory = containerProvider.Resolve<IParameterFactory>();
            var database = containerProvider.Resolve<IDatabaseProvider>();
            factory.Register("DatabaseSpellParameter", new DatabaseSpellParameter(await database.GetSpellDbcAsync()));
        }
    }
}