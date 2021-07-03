using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Module.Attributes;
using WDE.MySqlDatabaseCommon.Database;
using WDE.MySqlDatabaseCommon.Database.World;
using WDE.TrinityMySqlDatabase.Database;
using WDE.TrinityMySqlDatabase.Models;
using WDE.TrinityMySqlDatabase.Providers;

namespace WDE.TrinityMySqlDatabase
{
    [AutoRegister]
    [SingleInstance]
    public class WorldDatabaseProvider : WorldDatabaseDecorator
    {
        private readonly IStatusBar statusBar;

        public WorldDatabaseProvider(TrinityMySqlDatabaseProvider trinityDatabase,
            NullWorldDatabaseProvider nullWorldDatabaseProvider,
            IDatabaseSettingsProvider settingsProvider,
            IMessageBoxService messageBoxService,
            IStatusBar statusBar,
            ITaskRunner taskRunner) : base(nullWorldDatabaseProvider)
        {
            this.statusBar = statusBar;
            if (settingsProvider.Settings.IsEmpty)
                return;

            try
            {
                var cachedDatabase = new CachedDatabaseProvider(trinityDatabase, taskRunner, statusBar);
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