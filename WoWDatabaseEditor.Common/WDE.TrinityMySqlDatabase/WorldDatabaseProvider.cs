using System;
using Prism.Events;
using Prism.Ioc;
using WDE.Common.Database;
using WDE.Common.Events;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Module.Attributes;
using WDE.MySqlDatabaseCommon.Database.World;
using WDE.MySqlDatabaseCommon.Providers;

namespace WDE.TrinityMySqlDatabase
{
    [AutoRegister]
    [SingleInstance]
    public class WorldDatabaseProvider : WorldDatabaseDecorator
    {
        private readonly ILoadingEventAggregator loadingEventAggregator;
        private readonly IMainThread mainThread;

        public WorldDatabaseProvider(DatabaseResolver databaseResolver,
            NullWorldDatabaseProvider nullWorldDatabaseProvider,
            IWorldDatabaseSettingsProvider settingsProvider,
            IMessageBoxService messageBoxService,
            ILoadingEventAggregator loadingEventAggregator,
            IEventAggregator eventAggregator,
            IMainThread mainThread,
            IContainerProvider containerProvider) : base(nullWorldDatabaseProvider)
        {
            this.loadingEventAggregator = loadingEventAggregator;
            this.mainThread = mainThread;
            if (settingsProvider.Settings.IsEmpty)
            {
                PublishLoadedEvent();
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
                PublishLoadedEvent();
                messageBoxService.ShowDialog(new MessageBoxFactory<bool>().SetTitle("Database error")
                    .SetIcon(MessageBoxIcon.Error)
                    .SetMainInstruction("Couldn't connect to the database")
                    .SetContent(e.Message)
                    .WithOkButton(true)
                    .Build());
            }
        }

        private void PublishLoadedEvent()
        {
            mainThread.Dispatch(loadingEventAggregator.Publish<DatabaseLoadedEvent>);
        }
    }
}