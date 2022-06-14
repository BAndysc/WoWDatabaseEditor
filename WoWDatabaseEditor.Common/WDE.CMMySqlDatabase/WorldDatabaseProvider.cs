using System;
using Prism.Events;
using Prism.Ioc;
using WDE.Common.Database;
using WDE.Common.Events;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Module.Attributes;
using WDE.MySqlDatabaseCommon.Database.World;
using WDE.MySqlDatabaseCommon.Providers;

namespace WDE.CMMySqlDatabase
{
    [AutoRegister]
    [SingleInstance]
    public class WorldDatabaseProvider : WorldDatabaseDecorator
    {
        public WorldDatabaseProvider(DatabaseResolver databaseResolver,
            NullWorldDatabaseProvider nullWorldDatabaseProvider,
            IWorldDatabaseSettingsProvider settingsProvider,
            IMessageBoxService messageBoxService,
            ILoadingEventAggregator loadingEventAggregator,
            IEventAggregator eventAggregator,
            IContainerProvider containerProvider) : base(nullWorldDatabaseProvider)
        {
            if (settingsProvider.Settings.IsEmpty)
            {
                eventAggregator.GetEvent<AllModulesLoaded>().Subscribe(loadingEventAggregator.Publish<DatabaseLoadedEvent>, true);
                return;
            }

            try
            {
                var cachedDatabase = containerProvider.Resolve<CachedDatabaseProvider>((typeof(IAsyncDatabaseProvider), databaseResolver.ResolveWorld()));
                cachedDatabase.TryConnect();
                impl = cachedDatabase;
            }
            catch (Exception e)
            {
                impl = nullWorldDatabaseProvider;
                messageBoxService.ShowDialog(new MessageBoxFactory<bool>().SetTitle("Database error")
                    .SetIcon(MessageBoxIcon.Error)
                    .SetMainInstruction("Couldn't connect to the database")
                    .SetContent(e.Message)
                    .WithOkButton(true)
                    .Build());
            }
        }
    }
}