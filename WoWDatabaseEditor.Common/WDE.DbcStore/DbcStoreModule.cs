using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Prism.Events;
using Prism.Ioc;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.Events;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.DbcStore.Spells;
using WDE.DbcStore.Spells.Cataclysm;
using WDE.DbcStore.Spells.Legion;
using WDE.DbcStore.Spells.Tbc;
using WDE.DbcStore.Spells.Wrath;
using WDE.Module;
using WDE.Module.Attributes;
using WDE.MVVM.Observable;

namespace WDE.DbcStore
{
    [AutoRegister]
    public class DbcStoreModule : ModuleBase
    {
        private IContainerProvider containerProvider = null!;

        public override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            base.RegisterTypes(containerRegistry);
            containerRegistry.Register<IDbcSpellLoader, TbcSpellService>(nameof(TbcSpellService));
            containerRegistry.Register<IDbcSpellLoader, WrathSpellService>(nameof(WrathSpellService));
            containerRegistry.Register<IDbcSpellLoader, CataSpellService>(nameof(CataSpellService));
            containerRegistry.Register<IDbcSpellLoader, MopSpellService>(nameof(MopSpellService));
            containerRegistry.Register<IDbcSpellLoader, LegionSpellService>(nameof(LegionSpellService));
        }

        // this could be moved to somewhere else. ItemModule?
        public override void OnInitialized(IContainerProvider containerProvider)
        {
            this.containerProvider = containerProvider;
            containerProvider.Resolve<IEventAggregator>()
                .GetEvent<AllModulesLoaded>()
                .Subscribe(() =>
                    {
                        var factory = containerProvider.Resolve<IParameterFactory>();
                        factory.RegisterCombined("ItemParameter", "ItemDbcParameter", "ItemDatabaseParameter", (dbc, db) => new ItemParameter(dbc, db), QuickAccessMode.Limited);
                        factory.RegisterCombined("ItemCurrencyParameter", "ItemParameter", "CurrencyTypeParameter", (items, currencies) => new ItemOrCurrencyParameter(items, currencies), QuickAccessMode.None);
                    },
                    ThreadOption.PublisherThread,
                    true);

            containerProvider.Resolve<ILoadingEventAggregator>()
                .OnEvent<DatabaseLoadedEvent>()
                .SubscribeAction(_ =>
                {
                    LoadDatabaseItemsAsync().ListenErrors();
                    LoadSpawnGroupTemplates().ListenErrors();
                });
        }

        private async Task LoadDatabaseItemsAsync()
        {
            var factory = containerProvider.Resolve<IParameterFactory>();
            var database = containerProvider.Resolve<IDatabaseProvider>();
            factory.Register("ItemDatabaseParameter", new DatabaseItemParameter(await database.GetItemTemplatesAsync()));
        }
        
        private async Task LoadSpawnGroupTemplates()
        {
            var factory = containerProvider.Resolve<IParameterFactory>();
            var database = containerProvider.Resolve<IDatabaseProvider>();
            factory.Register("SpawnGroupTemplateParameter", new SpawnGroupTemplateParameter(await database.GetSpawnGroupTemplatesAsync()));
        }

        internal class SpawnGroupTemplateParameter : ParameterNumbered
        {
            public SpawnGroupTemplateParameter(IReadOnlyList<ISpawnGroupTemplate>? items)
            {
                if (items == null)
                    return;
                Items = new();
                foreach (var i in items)
                    Items.Add(i.Id, new SelectOption(i.Name));
            }
        }

        internal class DatabaseItemParameter : ParameterNumbered
        {
            public DatabaseItemParameter(IReadOnlyList<IItem>? items)
            {
                if (items == null)
                    return;
                Items = new();
                foreach (var i in items)
                    Items[i.Entry] = new SelectOption(i.Name);
            }
        }
        
        internal class ItemParameter : ParameterNumbered, IItemParameter
        {
            public ItemParameter(IParameter<long> dbc, IParameter<long> db)
            {
                Items = new();
                if (dbc.Items != null)
                {
                    foreach (var i in dbc.Items)
                        Items[i.Key] = i.Value;
                }
            
                if (db.Items != null)
                {
                    foreach (var i in db.Items)
                        Items[i.Key] = i.Value;
                }
            }
        }
        
        internal class ItemOrCurrencyParameter : ParameterNumbered, IItemParameter
        {
            public ItemOrCurrencyParameter(IParameter<long> items, IParameter<long> currencies)
            {
                Items = new();
            
                if (currencies.Items != null)
                {
                    foreach (var i in currencies.Items.Reverse())
                        Items[-i.Key] = i.Value;
                }
                
                if (items.Items != null)
                {
                    foreach (var i in items.Items)
                        Items[i.Key] = i.Value;
                }
            }

            public override string ToString(long key, ToStringOptions options)
            {
                if (options.withNumber)
                    return ToString(key);

                if (Items != null && Items.TryGetValue(key, out var option))
                    return option.Name;
                
                return "";
            }
        }
    }
}