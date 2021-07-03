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
using WDE.MySqlDatabaseCommon.Providers;
using WDE.SkyFireMySqlDatabase.Database;
using WDE.SkyFireMySqlDatabase.Models;
using WDE.SkyFireMySqlDatabase.Providers;

namespace WDE.SkyFireMySqlDatabase
{
    [AutoRegister]
    [SingleInstance]
    public class WorldDatabaseProvider : WorldDatabaseDecorator
    {
        public WorldDatabaseProvider(SkyFireMySqlDatabaseProvider skyfireDatabase,
            NullWorldDatabaseProvider nullWorldDatabaseProvider,
            IWorldDatabaseSettingsProvider settingsProvider,
            IMessageBoxService messageBoxService,
            IStatusBar statusBar,
            ITaskRunner taskRunner) : base(nullWorldDatabaseProvider)
        {
            if (settingsProvider.Settings.IsEmpty)
                return;

            try
            {
                var cachedDatabase = new CachedDatabaseProvider(skyfireDatabase, taskRunner, statusBar);
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